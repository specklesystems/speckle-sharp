using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Microsoft.CSharp.RuntimeBinder;
using Rhino.Display;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
#if RHINO8_OR_GREATER
using Grasshopper.Rhinoceros.Model;
#endif

namespace ConnectorGrasshopper.Extras;

public static class Utilities
{
  private static string dataTreePathPattern = @"^(@(\(\d+\))?)?(?<path>\{\d+(;\d+)*\})$";

  /// <summary>
  /// Gets the appropriate Grasshopper App name depending on the Version of Rhino currently running.
  /// </summary>
  /// <remarks>If running in Rhino >7, Rhino7 will be used as fallback.</remarks>
  /// <returns><see cref="VersionedHostApplications.Grasshopper7"/> when Rhino 7 is running, <see cref="VersionedHostApplications.Grasshopper8"/> when Rhino 8 is running, <see cref="VersionedHostApplications.Grasshopper6"/> otherwise.</returns>
  [Obsolete("Use Loader.GetGrasshopperHostAppVersion instead")]
  public static string GetVersionedAppName() => Loader.GetGrasshopperHostAppVersion();

  public static ISpeckleConverter GetDefaultConverter()
  {
    var n = SpeckleGHSettings.SelectedKitName;
    try
    {
      var defKit = KitManager
        .GetKitsWithConvertersForApp(GetVersionedAppName())
        .FirstOrDefault(kit => kit != null && kit.Name == n);
      var converter = defKit.LoadConverter(GetVersionedAppName());
      converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      converter.SetContextDocument(Loader.GetCurrentDocument());
      return converter;
    }
    catch
    {
      throw new Exception("Default kit was not found");
    }
  }

  /// <summary>
  ///    Converts a Grasshopper <see cref="DataTree{T}"/> to a <see cref="Base"/> object.
  ///    All paths in the <see cref="DataTree"/> will become the name of a property in the <see cref="Base"/> object,
  ///    with their value set as a <see cref="List{T}"/> containing the items of that path.
  /// </summary>
  /// <remarks>
  ///   Since the amount of items in each <see cref="DataTree{T}"/> path can greatly vary,
  ///   properties will be automatically detached.
  ///   This is done by adding a prefix to every path property following the pattern <c>@(DETACH_COUNT)</c>.
  /// </remarks>
  /// <example>
  ///   For the path <c >{0;0;0}</c> the resulting property name will be <c>@(1000){0;0;0}</c>.
  /// </example>
  /// <param name="dataInput">The input <see cref="GH_Structure{T}"/></param>
  /// <param name="converter">The converter to use for each path's items.</param>
  /// <param name="cancellationToken">The token to use for quick cancellation.</param>
  /// <param name="onConversionProgress">Optional action to report progress to the caller.</param>
  /// <param name="chunkLength">The length of the chunks to be created for each path list.</param>
  /// <returns>
  /// A new Base object where all dynamic properties have a list as value,
  /// and their names match the pattern described in the example.
  /// </returns>
  public static Base DataTreeToSpeckle(
    GH_Structure<IGH_Goo> dataInput,
    ISpeckleConverter converter,
    CancellationToken cancellationToken,
    Action onConversionProgress = null,
    int chunkLength = 1000
  )
  {
    var @base = new Base();

    foreach (var path in dataInput.Paths.ToList())
    {
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }

      var key = path.ToString();
      //TODO: Rolled back chunking due to issue with detaching children. Revisit this once done.
      var chunkingPrefix = "@";
      var value = dataInput.get_Branch(path);
      var converted = new List<object>();
      foreach (var item in value)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          break;
        }

        converted.Add(TryConvertItemToSpeckle(item, converter, true, onConversionProgress));
      }
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }

      @base[chunkingPrefix + key] = converted;
    }
    return @base;
  }

  /// <summary>
  ///   Converts a <see cref="Base"/> object into a Grasshopper <see cref="DataTree{T}"/>.
  /// </summary>
  /// <param name="base">The object to convert</param>
  /// <param name="converter">The converter to use for the child items</param>
  /// <param name="OnConversionProgress">Optional action to report any progress if necessary.</param>
  /// <returns></returns>
  public static GH_Structure<IGH_Goo> DataTreeToNative(
    Base @base,
    ISpeckleConverter converter,
    Action onConversionProgress = null
  )
  {
    var dataTree = new GH_Structure<IGH_Goo>();
    @base
      .GetDynamicMembers()
      .ToList()
      .ForEach(key =>
      {
        var value = @base[key] as List<object>;
        var path = new GH_Path();
        var pattern = new Regex(dataTreePathPattern); // Match for the dynamic detach magic "@(DETACH_INT)PATH"
        var matchRes = pattern.Match(key);
        if (matchRes.Length == 0)
        {
          return;
        }

        var pathKey = matchRes.Groups["path"].Value;
        var res = path.FromString(pathKey);
        if (!res)
        {
          return;
        }

        var converted = value.Select(item => TryConvertItemToNative(item, converter));
        dataTree.AppendRange(converted, path);
      });

    return dataTree;
  }

  /// <summary>
  ///   Checks if a given <see cref="Base"/> object can be converted into a Grasshopper <see cref="DataTree{T}"/>.
  /// </summary>
  /// <param name="base">The base object to test for DataTree conversion.</param>
  /// <remarks>
  ///   This will check if all Dynamic Members of a <see cref="Base"/> object match to the pattern "{\d+(;\d+)*}" (i.e. {0,0}, {123;4;22}, etc.)
  /// </remarks>
  /// <returns>True if the <see cref="Base"/> object will be successfully converted into a <see cref="DataTree{T}"/>, false otherwise.</returns>
  public static bool CanConvertToDataTree(Base @base)
  {
    var regex = new Regex(dataTreePathPattern);
    var dynamicMembers = @base.GetDynamicMembers().ToList();
    if (dynamicMembers.Count == 0)
    {
      return false;
    }

    var isDataTree = dynamicMembers.All(el => regex.Match(el).Success);
    return isDataTree;
  }

  public static List<object> DataTreeToNestedLists(
    GH_Structure<IGH_Goo> dataInput,
    ISpeckleConverter converter,
    Action OnConversionProgress = null
  )
  {
    return DataTreeToNestedLists(dataInput, converter, CancellationToken.None, OnConversionProgress);
  }

  public static List<object> DataTreeToNestedLists(
    GH_Structure<IGH_Goo> dataInput,
    ISpeckleConverter converter,
    CancellationToken cancellationToken,
    Action OnConversionProgress = null
  )
  {
    var output = new List<object>();
    for (var i = 0; i < dataInput.Branches.Count; i++)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        return output;
      }

      var path = dataInput.Paths[i].Indices.ToList();
      var leaves = new List<object>();

      foreach (var goo in dataInput.Branches[i])
      {
        if (cancellationToken.IsCancellationRequested)
        {
          return output;
        }

        OnConversionProgress?.Invoke();
        leaves.Add(TryConvertItemToSpeckle(goo, converter, true));
      }
      RecurseTreeToList(output, path, 0, leaves);
    }
    OnConversionProgress?.Invoke();

    return output;
  }

  private static void RecurseTreeToList(List<object> parent, List<int> path, int pathIndex, List<object> objects)
  {
    var listIndex = path[pathIndex]; //there should be a list at this index inside this parent list

    parent = EnsureHasSublistAtIndex(parent, listIndex);
    var sublist = parent[listIndex] as List<object>;
    //it's the last index of the path => the last sublist => add objects
    if (pathIndex == path.Count - 1)
    {
      sublist.AddRange(objects);
      return;
    }

    RecurseTreeToList(sublist, path, pathIndex + 1, objects);
  }

  /// <summary>
  /// Wraps an object in the appropriate <see cref="IGH_Goo"/> subtype for display in GH. The default value will return a <see cref="GH_ObjectWrapper"/> instance.
  /// </summary>
  /// <param name="obj">Object to be wrapped.</param>
  /// <returns>An <see cref="IGH_Goo"/> instance wrapping the object.</returns>
  public static IGH_Goo WrapInGhType(object obj)
  {
    switch (obj)
    {
      case Base @base:
        return new GH_SpeckleBase(@base);
      case DisplayMaterial dm:
        return new GH_Material(dm);
      case Color c:
        return new GH_Colour(c);
      case Transform t:
        return new GH_Transform(t);
      case GH_ObjectWrapper ow:
        return WrapInGhType(ow.Value); // Unwrap generic object wrappers and try to make them specific.
      case IGH_Goo goo:
        return goo; // Assume any other IGH_Goo is properly wrapped
      default:
        return GH_Convert.ToGoo(obj) ?? new GH_ObjectWrapper(obj); // Ensure that a GH_Goo is always returned
    }
  }

  /// <summary>
  /// For a given parent list it creates enough sublists so that we have a sublist at the specified index
  /// If the parent contains some objects already, insert the sublist the the specified index
  /// If there is a missing branch, insert an empty list {0,0} {0,1} {0,3}
  /// If paths have variable length account for it {0} {0,0} {0,1,0}
  /// </summary>
  /// <param name="parent"></param>
  /// <param name="index"></param>
  /// <returns></returns>
  private static List<object> EnsureHasSublistAtIndex(List<object> parent, int index)
  {
    while (parent.Count <= index || !(parent[index] is List<object>))
    {
      if (parent.Count > index)
      {
        parent.Insert(index, new List<object>());
      }
      else
      {
        parent.Add(new List<object>());
      }
    }

    return parent;
  }

  /// <summary>
  /// Traverses all keys of a given <see cref="Base"/> instance and attempts to convert any entities 'To Native'.
  /// This method works recursively, and will traverse any <see cref="Base"/> instances it encounters that it cannot convert directly.
  /// </summary>
  /// <param name="base">Base object</param>
  /// <param name="converter">Converter instance to use.</param>
  /// <returns>A shallow copy of the base object with compatible values converted to native (Rhino) entities.</returns>
  public static Base TraverseAndConvertToNative(Base @base, ISpeckleConverter converter)
  {
    var copy = @base.ShallowCopy();
    copy.GetMembers()
      .ToList()
      .ForEach(keyval =>
      {
        // TODO: Handle dicts!!!
        if (keyval.Value is IList list)
        {
          var converted = new List<object>();
          foreach (var item in list)
          {
            var goo = TryConvertItemToNative(item, converter, true);
            var value = goo.GetType().GetProperty("Value")?.GetValue(goo);
            converted.Add(value);
          }

          copy[keyval.Key] = converted;
        }
        else if (typeof(IDictionary).IsAssignableFrom(keyval.Value.GetType()))
        {
          var converted = new Dictionary<string, object>();
          foreach (DictionaryEntry kvp in keyval.Value as IDictionary)
          {
            converted[kvp.Key.ToString()] = TryConvertItemToNative(kvp.Value, converter, true);
          }

          copy[keyval.Key] = converted;
        }
        else
        {
          var goo = TryConvertItemToNative(keyval.Value, converter, true);
          var value = goo.GetType().GetProperty("Value")?.GetValue(goo);
          copy[keyval.Key] = value;
        }
      });
    return copy;
  }

  /// <summary>
  /// Traverses all keys of a given <see cref="Base"/> instance and attempts to convert any entities 'To Speckle'.
  /// This method works recursively, and will traverse any Base instances it encounters that it cannot convert directly.
  /// </summary>
  /// <param name="base">Base object</param>
  /// <param name="converter">Converter instance to use.</param>
  /// <returns>A shallow copy of the base object with compatible values converted to Speckle entities.</returns>
  public static Base TraverseAndConvertToSpeckle(
    Base @base,
    ISpeckleConverter converter,
    Action OnConversionProgress = null
  )
  {
    var subclass = @base.GetType().IsSubclassOf(typeof(Base));
    if (subclass)
    {
      return @base;
    }

    var copy = @base.ShallowCopy();
    var keyValuePairs = copy.GetMembers().ToList();
    keyValuePairs.ForEach(keyval =>
    {
      // TODO: Handle dicts!!
      var value = keyval.Value;
      if (value == null)
      {
        // TODO: Handle null values in properties here. For now, we just ignore that prop in the object
        copy[keyval.Key] = null;
        return;
      }
      if (value is IList list)
      {
        var converted = new List<object>();
        foreach (var item in list)
        {
          var conv = TryConvertItemToSpeckle(item, converter, true);
          converted.Add(conv);
        }

        copy[keyval.Key] = converted;
      }
      else if (typeof(IDictionary).IsAssignableFrom(value.GetType()))
      {
        var converted = new Dictionary<string, object>();
        foreach (DictionaryEntry kvp in value as IDictionary)
        {
          converted[kvp.Key.ToString()] = TryConvertItemToSpeckle(kvp.Value, converter, true);
        }

        copy[keyval.Key] = converted;
      }
      else
      {
        copy[keyval.Key] = TryConvertItemToSpeckle(value, converter, true);
      }
    });
    return copy;
  }

  /// <summary>
  /// Try to convert a given object into native (Grasshopper) format.
  /// </summary>
  /// <param name="value">Object to convert</param>
  /// <param name="converter">Converter instance to use.</param>
  /// <param name="recursive">Indicates if any non-convertible <see cref="Base"/> instances should be traversed too.</param>
  /// <returns>An <see cref="IGH_Goo"/> instance holding the converted object. </returns>
  public static IGH_Goo TryConvertItemToNative(object value, ISpeckleConverter converter, bool recursive = false)
  {
    if (converter == null)
    {
      return WrapInGhType(value);
    }

    if (value == null)
    {
      return null;
    }

    if (value is IGH_Goo)
    {
      value = value.GetType().GetProperty("Value")?.GetValue(value);
    }

    if (value is Base @base)
    {
      if (converter.CanConvertToNative(@base))
      {
        try
        {
          var converted = converter.ConvertToNative(@base);
          return WrapInGhType(converted);
        }
        catch (SpeckleException ex)
        {
          converter.Report.ConversionErrors.Add(new SpeckleException($"Could not convert {@base}", ex));
          return null;
        }
      }

      if (recursive)
      {
        // Object is base but cannot convert directly, traverse!!!
        var x = TraverseAndConvertToNative(@base, converter);
        return new GH_SpeckleBase(x);
      }
    }

    if (value is Base base2)
    {
      return new GH_SpeckleBase { Value = base2 };
    }

    if (value.GetType().IsSimpleType())
    {
      return GH_Convert.ToGoo(value);
    }

    if (value is Enum)
    {
      var i = (Enum)value;
      return new GH_ObjectWrapper { Value = i };
    }
    return WrapInGhType(value);
  }

  /// <summary>
  /// Try to convert a given object into native (Rhino) format.
  /// </summary>
  /// <param name="value">Object to convert</param>
  /// <param name="converter">Converter instance to use.</param>
  /// <param name="recursive">Indicates if any non-convertible <see cref="Base"/> instances should be traversed too.</param>
  /// <returns>An <see cref="IGH_Goo"/> instance holding the converted object. </returns>
  public static object TryConvertItemToSpeckle(
    object value,
    ISpeckleConverter converter,
    bool recursive = false,
    Action OnConversionProgress = null
  )
  {
    if (value is null)
    {
      return null;
    }

    value = UnwrapRhino8Object(value);
    if (value is IGH_Goo)
    {
      var valuePropInfo = value
        .GetType()
        .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (valuePropInfo != null)
      {
        var tempValue = valuePropInfo.GetValue(value);
        if (tempValue != null)
        {
          value = tempValue;
        }
      }
    }

    if (value.GetType().IsSimpleType())
    {
      return value;
    }

    if (converter != null && converter.CanConvertToSpeckle(value))
    {
      var result = converter.ConvertToSpeckle(value);
      if (result != null)
      {
        string refId = GetRefId(value);
        result.applicationId = refId;
      }

      return result;
    }

    var subclass = value.GetType().IsSubclassOf(typeof(Base));
    if (subclass)
    {
      // TODO: Traverse through dynamic props only.
      return value;
    }

    if (recursive && value is Base @base)
    {
      return TraverseAndConvertToSpeckle(@base, converter);
    }

    if (value is Base base2)
    {
      return base2;
    }

    return null;
  }

  private static object UnwrapRhino8Object(object value)
  {
#if RHINO8_OR_GREATER
    // INFO: Fill in here as we enable conversion for other non-model object types.
    switch (value)
    {
      case ModelObject modelObject:
      {
        var propInfo = value
          .GetType()
          .GetProperty("Geometry", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return propInfo?.GetValue(value) ?? value;
      }
      default:
        return value;
    }
#else
    return value;
#endif
  }

  public static string GetRefId(object value)
  {
    dynamic r = value;
    string refId = null;

    try
    {
      if (r.IsReferencedGeometry)
      {
        refId = r.ReferenceID.ToString();
      }
    }
    catch (RuntimeBinderException)
    {
      // Pass
    }

    //switch (value)
    //{
    //  case GH_GeometricGoo<> r:
    //    if (r.IsReferencedGeometry)
    //      refId = r.ReferenceID.ToString();
    //    break;
    //  case GH_Mesh r:
    //    if (r.IsReferencedGeometry)
    //      refId = r.ReferenceID.ToString();
    //    break;
    //  case GH_Line r:
    //    if (r.IsReferencedGeometry)
    //      refId = r.ReferenceID.ToString();
    //    break;
    //  case GH_Point r:
    //    if (r.IsReferencedGeometry)
    //      refId = r.ReferenceID.ToString();
    //    break;
    //  case GH_Surface r:
    //    if (r.IsReferencedGeometry)
    //      refId = r.ReferenceID.ToString();
    //    break;
    //  case GH_Curve r:
    //    if (r.IsReferencedGeometry)
    //      refId = r.ReferenceID.ToString();
    //    break;
    //}
    return refId;
  }

  /// <summary>
  /// Get all descendant branches of a specific path in a tree.
  /// </summary>
  /// <param name="valueTree"></param>
  /// <param name="searchPath"></param>
  /// <returns></returns>
  public static GH_Structure<IGH_Goo> GetSubTree(GH_Structure<IGH_Goo> valueTree, GH_Path searchPath)
  {
    var subTree = new GH_Structure<IGH_Goo>();
    var gen = 0;
    foreach (var path in valueTree.Paths)
    {
      var branch = valueTree.get_Branch(path) as IEnumerable<IGH_Goo>;
      if (path.IsAncestor(searchPath, ref gen))
      {
        subTree.AppendRange(branch, path);
      }
      else if (path.IsCoincident(searchPath))
      {
        subTree.AppendRange(branch, path);
        break;
      }
    }

    subTree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);
    return subTree;
  }

  public static bool IsList(object @object)
  {
    var type = @object.GetType();
    return typeof(IEnumerable).IsAssignableFrom(type)
      && !typeof(IDictionary).IsAssignableFrom(type)
      && type != typeof(string);
  }

  /// <summary>
  /// Workflow for casting a Base into a GH_Structure, taking into account potential Convertions. Pass Converter as null if you don't care for conversion.
  /// </summary>
  /// <param name="Converter"></param>
  /// <param name="base"></param>
  /// <returns></returns>
  public static GH_Structure<IGH_Goo> ConvertToTree(
    ISpeckleConverter Converter,
    Base @base,
    Action<GH_RuntimeMessageLevel, string> onError = null,
    bool unwrap = false
  )
  {
    var data = new GH_Structure<IGH_Goo>();

    // Use the converter
    // case 1: it's an item that has a direct conversion method, eg a point
    if (Converter != null && Converter.CanConvertToNative(@base))
    {
      var converted = Converter.ConvertToNative(@base);
      data.Append(WrapInGhType(converted));
    }
    else if (unwrap && @base.GetDynamicMembers().Count() == 1 && (@base["@data"] != null || @base["@Data"] != null))
    {
      // Comes from a wrapper
      var wrappedData = @base["@data"] ?? @base["@Data"];
      if (wrappedData is Base wrappedBase)
      {
        // New object type tree
        data = DataTreeToNative(wrappedBase, Converter);
      }
      else if (wrappedData is IList list)
      {
        // Old nested list type.
        var treeBuilder = new TreeBuilder(Converter);
        data = treeBuilder.Build(list);
      }
    }
    // Simple pass the SpeckleBase
    else
    {
      if (onError != null)
      {
        onError(GH_RuntimeMessageLevel.Remark, "This object needs to be expanded.");
      }

      data.Append(new GH_SpeckleBase(@base));
    }
    return data;
  }
}
