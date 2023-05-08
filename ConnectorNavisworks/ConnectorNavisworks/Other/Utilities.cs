using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks;

internal static class ArrayExtension
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static T[] ToArray<T>(this Array arr)
    where T : struct
  {
    var result = new T[arr.Length];
    Array.Copy(arr, result, result.Length);
    return result;
  }
}

internal sealed class PseudoIdComparer : IComparer<string>
{
  public int Compare(string x, string y)
  {
    return x != null && y != null
      ? x.Length == y.Length
        ? string.Compare(x, y, StringComparison.Ordinal)
        : x.Length.CompareTo(y.Length)
      : 0;
  }
}

public static class Utilities
{
#if NAVMAN21
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2024);
#elif NAVMAN20
  public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2023);
#elif NAVMAN19
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2022);
#elif NAVMAN18
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2021);
#elif NAVMAN17
    public readonly static string VersionedAppName = HostApplications.Navisworks.GetVersion(HostAppVersion.v2020);
#endif

  private const string RootNodePseudoId = "___"; // This should be shorter than the padding on indexes and not contain '-'

  public static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue)
  {
    Console.WriteLine(message, color);
  }

  public static void WarnLog(string warningMessage)
  {
    ConsoleLog(warningMessage, ConsoleColor.DarkYellow);
  }

  public static void ErrorLog(Exception err)
  {
    ErrorLog(err.Message);
    throw err;
  }

  public static void ErrorLog(string errorMessage)
  {
    ConsoleLog(errorMessage, ConsoleColor.DarkRed);
  }

  public static string GetUnits(Document doc)
  {
    return doc.Units.ToString();
  }

  internal static string ObjectDescriptor(string pseudoId)
  {
    var element = PointerToModelItem(pseudoId);
    var simpleType = element
      .GetType()
      .ToString()
      .Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault();
    return string.IsNullOrEmpty(element.ClassDisplayName)
      ? $"{simpleType}"
      : $"{simpleType} {element.ClassDisplayName}";
  }

  internal static ModelItem PointerToModelItem(object @string)
  {
    int[] pathArray;

    if (@string.ToString() == RootNodePseudoId)
    {
      var rootItems = Application.ActiveDocument.Models.RootItems;

      return rootItems.First;
    }

    try
    {
      pathArray = @string
        .ToString()
        .Split('-')
        .Select(x =>
        {
          if (int.TryParse(x, out var value))
            return value;

          throw new ArgumentException("malformed path pseudoId");
        })
        .ToArray();
    }
    catch (ArgumentException)
    {
      return null;
    }

    var oState = ComApiBridge.State;
    var protoPath = (InwOaPath)oState.ObjectFactory(nwEObjectType.eObjectType_nwOaPath);

    var oneBasedArray = Array.CreateInstance(
      typeof(int),
      // ReSharper disable once RedundantExplicitArraySize
      new int[1] { pathArray.Length },
      // ReSharper disable once RedundantExplicitArraySize
      new int[1] { 1 }
    );

    Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);

    protoPath.ArrayData = oneBasedArray;

    var m = ComApiBridge.ToModelItem(protoPath);

    return m;
  }

  // The path for ModelItems is their node position at each level of the Models tree.
  // This is the defacto UID for that element within the file at that time.
  public static string GetPseudoId(object input)
  {
    int[] arrayData;
    switch (input)
    {
      case ModelItem modelItem:
        arrayData = ((Array)ComApiBridge.ToInwOaPath(modelItem).ArrayData).ToArray<int>();
        break;

      // Index path is used by SelectionSets and SavedViewpoints - it can try to find the item using the ResolveIndexPath method
      case Collection<int> indexPath:
        arrayData = indexPath.ToArray();
        break;
      case InwOaPath path:
        arrayData = ((Array)path.ArrayData).ToArray<int>();
        break;
      case int[] indices:
        arrayData = indices;
        break;
      default:
        throw new ArgumentException("Invalid input type, expected ModelItem, InwOaPath, Collection<int> or int[]");
    }

    // Neglect the Root Node
    // Acknowledging that if a collection contains >=10000 children then this indexing will be inadequate
    return arrayData.Length == 0
      ? RootNodePseudoId
      : string.Join("-", arrayData.Select(x => x.ToString().PadLeft(4, '0')));
  }

  /// <summary>
  /// Builds a nested object hierarchy from a dictionary, where each entry in the dictionary
  /// represents an object and its parent's identifier.
  /// </summary>
  /// <param name="dictionary">
  /// A dictionary containing objects and their parent identifiers as a tuple.
  /// The key of the dictionary is the object identifier, and the value is a tuple of (object, parent identifier).
  /// </param>
  /// <returns>A list of root objects, each containing its nested children objects.</returns>
  internal static List<Base> BuildNestedObjectHierarchy(Dictionary<string, Tuple<Base, string>> dictionary)
  {
    // Group dictionary entries by their parent identifier
    var parentGroups = dictionary.Values.Where(x => !string.IsNullOrEmpty(x.Item2)).GroupBy(x => x.Item2);

    // Initialize a HashSet to store root objects
    var rootSet = new HashSet<Base>();

    foreach (var group in parentGroups)
    {
      // If parent not found, add all items in the group to rootSet
      if (!dictionary.TryGetValue(group.Key, out var parentTuple))
      {
        foreach ((Base item, string parentId) in group)
        {
          if (item is Collection collection)
          {
            var childEntries = dictionary.Values.Where(x => x.Item2 == parentId).ToList();
            collection.elements = childEntries.Select(x => x.Item1).ToList();

            // Remove child entries from the dictionary that are not collections
            foreach (var childEntry in childEntries.Where(x => !(x.Item1 is Collection)))
              dictionary.Remove((string)childEntry.Item1["applicationId"]);
          }

          rootSet.Add(item);
        }

        continue;
      }

      // Get the parent object and create a list for its children
      var parent = parentTuple.Item1;

      var childList = new List<Base>();

      // Iterate through the children and add them to the child list
      foreach (var child in group)
      {
        if (child.Item1 is not Collection)
          dictionary.Remove((string)child.Item1["applicationId"]);

        childList.Add(child.Item1);
      }

      // Assign the child list to the parent object
      ((Collection)parent).elements = childList;
    }

    // Identify and remove entries that have a non-empty parent identifier and exist in the dictionary
    var entriesToRemove = dictionary.Values
      .Where(x => !string.IsNullOrEmpty(x.Item2) && dictionary.ContainsKey(x.Item2))
      .ToList();

    foreach (var entryToRemove in entriesToRemove)
      dictionary.Remove((string)entryToRemove.Item1["applicationId"]);

    // Add remaining dictionary values to the root set
    rootSet.UnionWith(dictionary.Values.Select(x => x.Item1));

    return rootSet.ToList();
  }
}
