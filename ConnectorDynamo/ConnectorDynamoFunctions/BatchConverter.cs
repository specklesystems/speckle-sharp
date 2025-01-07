using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.ConnectorDynamo.Functions;

[IsVisibleInDynamoLibrary(false)]
public class BatchConverter
{
  private ISpeckleConverter _converter { get; set; }
  private ISpeckleKit _kit { get; set; }

  public EventHandler<OnErrorEventArgs> OnError;

  public BatchConverter()
  {
    var kit = KitManager.GetDefaultKit();
    _kit = kit ?? throw new SpeckleException("Cannot find the Objects Kit. Has it been copied to the Kits folder?");
    _converter = kit.LoadConverter(Utils.GetAppName());

    if (_converter == null)
    {
      throw new SpeckleException("Cannot find the Dynamo converter. Has it been copied to the Kits folder?");
    }

    // if in Revit, we have a doc, injected by the Extension
    if (Globals.RevitDocument != null)
    {
      _converter.SetContextDocument(Globals.RevitDocument);
    }
  }

  /// <summary>
  /// Helper method to convert a tree-like structure (nested lists) to Speckle
  /// </summary>
  /// <param name="object"></param>
  /// <returns></returns>
  public Base ConvertRecursivelyToSpeckle(object @object)
  {
    if (@object is ProtoCore.DSASM.StackValue)
    {
      throw new SpeckleException("Invalid input");
    }

    var converted = RecurseTreeToSpeckle(@object);

    if (converted is null)
    {
      return null;
    }

    var @base = new Base();

    //case 1: lists and basic types => add them to a wrapper Base object in a `data` prop
    //case 2: Base => just use it as it is
    if (IsList(converted) || converted.GetType().IsSimpleType())
    {
      @base["@data"] = converted;
    }
    else if (converted is Base convertedBase)
    {
      @base = convertedBase;
    }

    return @base;
  }

  private object RecurseTreeToSpeckle(object @object)
  {
    if (IsList(@object))
    {
      var list = ((IEnumerable)@object).Cast<object>().ToList();
      return list.Select(RecurseTreeToSpeckle).ToList();
    }

    if (@object is DesignScript.Builtin.Dictionary dsDic)
    {
      return DictionaryToBase(dsDic);
    }

    if (@object is Dictionary<string, object> dic)
    {
      return DictionaryToBase(dic);
    }

    //is item
    return TryConvertItemToSpeckle(@object);
  }

  private static Dictionary<string, object> DynamoDictionaryToDictionary(DesignScript.Builtin.Dictionary dsDic)
  {
    var dict = new Dictionary<string, object>();

    dsDic
      .Keys.ToList()
      .ForEach(key =>
      {
        dict[key] = dsDic.ValueAtKey(key);
      });
    return dict;
  }

  private Base DictionaryToBase(DesignScript.Builtin.Dictionary dsDic)
  {
    return DictionaryToBase(DynamoDictionaryToDictionary(dsDic));
  }

  private Base DictionaryToBase(Dictionary<string, object> dic)
  {
    var hasSpeckleType = dic.Keys.Contains("speckle_type");
    var @base = new Base();
    var type = @base.GetType();
    if (hasSpeckleType)
    {
      // If the dictionary contains a `speckle_type` key, try to find and create an instance of that type
      var s = dic["speckle_type"] as string;

      var baseType = typeof(Base);
      type = _kit.Types.FirstOrDefault(t => t.FullName == s) ?? baseType;
      if (type != baseType)
      {
        @base = Activator.CreateInstance(type) as Base;
      }
    }
    var regex = new Regex("//");
    foreach (var key in dic.Keys)
    {
      // Dynamo does not support `::` in dictionary keys. We use `//` instead.
      // Upon send, any `//` must be replaced by `::` again.
      var replacedKey = regex.Replace(key, "::");

      var propInfo = type.GetProperty(key);
      var convertedValue = RecurseTreeToSpeckle(dic[key]);

      if (propInfo == null)
      {
        // Key is dynamic, just add it as is
        @base[replacedKey] = convertedValue;
        continue;
      }
      // It's an instance field, check if we can set it
      if (!propInfo.CanWrite)
      {
        continue;
      }

      // Check if it's a list, and if so, try to create the according typed instance.
      if (IsList(convertedValue))
      {
        var list = convertedValue as IList;
        var genericTypeDefinition = propInfo.PropertyType;
        var typedValue = Activator.CreateInstance(genericTypeDefinition) as IList;
        foreach (var item in list)
        {
          typedValue.Add(item);
        }
        convertedValue = typedValue;
      }
      @base[replacedKey] = convertedValue;
    }

    return @base;
  }

  private object TryConvertItemToSpeckle(object value)
  {
    object result = null;
    if (_converter.CanConvertToSpeckle(value))
    {
      try
      {
        return _converter.ConvertToSpeckle(value);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        var spcklEx = new SpeckleException($"Could not convert {value.GetType().Name} to Speckle:", ex);
        OnError?.Invoke(this, new OnErrorEventArgs(spcklEx));
        return null;
      }
    }

    if (value is Base || value is null || value.GetType().IsSimpleType())
    {
      return value;
    }

    return result;
  }

  private static Regex dataTreePathRegex => new(@"^(@(\(\d+\))?)?(?<path>\{\d+(;\d+)*\})$");

  public static bool IsDataTree(Base @base)
  {
    var regex = dataTreePathRegex;
    var members = @base.GetMembers(DynamicBaseMemberType.Dynamic).Keys.ToList();
    if (members.Count == 0)
    {
      return false;
    }

    var isDataTree = members.All(el => regex.Match(el).Success);
    return members.Count > 0 && isDataTree;
  }

  public object ConvertDataTreeToNative(Base @base)
  {
    var names = @base.GetMembers(DynamicBaseMemberType.Dynamic).Keys.ToList();
    var list = new List<object>();
    foreach (var name in names)
    {
      if (!dataTreePathRegex.Match(name).Success)
      {
        continue; // Ignore non matching elements, done for extra safety.
      }

      var parts = name.Split('{')[1] // Get everything after open curly brace
        .Split('}')[0] // Get everything before close curly brace
        .Split(';') // Split by ;
        .Select(text =>
        {
          // At this point, we expect split to yield all integers based on the dataTreePathRegex check above.
          _ = int.TryParse(text, out var num);
          return num;
        })
        .ToList();

      var currentList = list;
      foreach (var p in parts)
      {
        while (currentList.Count < p + 1)
        {
          var newList = new List<object>();
          currentList.Add(newList);
        }

        currentList = currentList[p] as List<object>;
      }

      var value = @base[name];
      var converted = RecurseTreeToNative(value) as List<object>;
      currentList.AddRange(converted);
      Console.WriteLine(parts);
    }
    return list;
  }

  /// <summary>
  /// Helper method to convert a tree-like structure (nested lists) to Native
  /// </summary>
  /// <param name="base"></param>
  /// <returns></returns>
  public object ConvertRecursivelyToNative(Base @base)
  {
    if (@base == null)
    {
      return null;
    }

    // case 1: it's an item that has a direct conversion method, eg a point
    if (_converter.CanConvertToNative(@base))
    {
      return TryConvertItemToNative(@base);
    }

    // case 2: it's a wrapper Base
    //       2a: if there's only one member unpack it
    //       2b: otherwise return dictionary of unpacked members
    var members = @base
      .GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic | DynamicBaseMemberType.SchemaComputed)
      .Keys.ToList();

    if (members.Count == 1)
    {
      var converted = RecurseTreeToNative(@base[members.ElementAt(0)]);
      return converted;
    }

    var regex = new Regex("::");
    var dict = members.ToDictionary(x => regex.Replace(x, "//"), x => RecurseTreeToNative(@base[x]));
    return dict;
  }

  private object RecurseTreeToNative(object @object)
  {
    if (IsList(@object))
    {
      var list = ((IEnumerable)@object).Cast<object>();
      return list.Select(RecurseTreeToNative).ToList();
    }
    if (@object is Base @base && IsDataTree(@base))
    {
      return ConvertDataTreeToNative(@base);
    }

    return TryConvertItemToNative(@object);
  }

  private object TryConvertItemToNative(object value)
  {
    if (value == null)
    {
      return null;
    }

    //it's a simple type or not a Base
    if (value.GetType().IsSimpleType() || !(value is Base))
    {
      return value;
    }

    var @base = (Base)value;
    //it's an unsupported Base, return a dictionary
    if (!_converter.CanConvertToNative(@base))
    {
      return @base
        .GetMembers(
          DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic | DynamicBaseMemberType.SchemaComputed
        )
        .ToList()
        .ToDictionary(pair => pair.Key, pair => RecurseTreeToNative(pair.Value));
    }

    try
    {
      return _converter.ConvertToNative(@base);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error("Could not convert {typeName}(id={id}", @base.GetType().Name, @base.id);
      var spcklError = new SpeckleException($"Could not convert {@base.GetType().Name}(id={@base.id}) to Dynamo.", ex);
      OnError?.Invoke(this, new OnErrorEventArgs(spcklError));
      return null;
    }
  }

  public static bool IsList(object @object)
  {
    if (@object == null)
    {
      return false;
    }

    var type = @object.GetType();
    return (
      typeof(IEnumerable).IsAssignableFrom(type)
      && !typeof(IDictionary).IsAssignableFrom(type)
      && type != typeof(string)
    );
  }

  public static bool IsDictionary(object @object)
  {
    if (@object == null)
    {
      return false;
    }

    Type type = @object.GetType();
    return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
  }
}
