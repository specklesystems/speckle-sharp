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

  internal static List<Base> NestDictionaryEntries(Dictionary<string, Tuple<Base, string>> dictionary)
  {
    // First, group the entries by their parent IDs
    var parentGroups = dictionary.Values.Where(b => !string.IsNullOrEmpty(b.Item2)).GroupBy(b => b.Item2);

    // Create a new list for entries with no parent
    var rootSet = new HashSet<Base>();

    // Iterate over the parent groups and construct nested Collections
    foreach (var group in parentGroups)
    {
      // Find the parent Collection for this group
      if (!dictionary.TryGetValue(group.Key, out var parentTuple))
      {
        // If the parent doesn't exist, add the child entries to the root list as bare objects
        foreach ((Base @base, string parentId) in group)
        {
          if (@base is Collection collection)
          {
            var childChildren = dictionary.Values.Where(b => b.Item2 == parentId).ToList();
            collection.elements = childChildren.Select(b => b.Item1).ToList();

            foreach (var ccg in childChildren.Where(ccg => ccg.Item1 is not Collection))
            {
              dictionary.Remove((string)ccg.Item1["applicationId"]);
            }
          }

          rootSet.Add(@base);
        }
        continue;
      }

      var parent = parentTuple.Item1;

      // Create a new list for the child entries
      var childList = new List<Base>();

      // Add each child entry to the child list and remove it from the dictionary
      foreach (var child in group)
      {
        if (child.Item1 is not Collection)
          dictionary.Remove((string)child.Item1["applicationId"]);
        childList.Add(child.Item1);
      }

      // Set the parent's "elements" property to the child list
      ((Collection)parent).elements = childList;
    }

    // now remove all the dictionary values that have a parent id also in the dictionary
    // this will leave only the root elements in the dictionary

    var objectsToRemove = dictionary.Values
      .Where(b => !string.IsNullOrEmpty(b.Item2) && dictionary.ContainsKey(b.Item2))
      .ToList();

    foreach (Tuple<Base, string> removeCandidate in objectsToRemove)
    {
      dictionary.Remove((string)removeCandidate.Item1["applicationId"]);
    }

    // Add any remaining entries in the dictionary to the root set as bare objects
    rootSet.UnionWith(dictionary.Values.Select(t => t.Item1));

    // Convert the root set to a list and return it
    return rootSet.ToList();
  }
}
