using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks.Other;

public class Element
{
  private ModelItem _modelItem;

  public static Element GetElement(string pseudoId)
  {
    return new Element(pseudoId);
  }

  public Element GetElement(ModelItem modelItem)
  {
    return new Element(GetPseudoId(modelItem)) { _modelItem = modelItem };
  }

  public ModelItem ModelItem => Resolve();
  public string PseudoId { get; private set; }

  private Element(string pseudoId)
  {
    PseudoId = pseudoId;
  }

  public Element() { }

  private string GetPseudoId(ModelItem modelItem)
  {
    if (PseudoId != null)
      return PseudoId;

    var arrayData = ((Array)ComApiBridge.ToInwOaPath(modelItem).ArrayData).ToArray<int>();
    PseudoId =
      arrayData.Length == 0
        ? Constants.RootNodePseudoId
        : string.Join("-", arrayData.Select(x => x.ToString().PadLeft(4, '0')));
    return PseudoId;
  }

  private ModelItem Resolve()
  {
    if (_modelItem != null)
      return _modelItem;

    if (PseudoId == null)
      return null;

    int[] pathArray;

    if (PseudoId == Constants.RootNodePseudoId)
    {
      var rootItems = Application.ActiveDocument.Models.RootItems;
      return rootItems.First;
    }

    try
    {
      pathArray = PseudoId
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

    _modelItem = ComApiBridge.ToModelItem(protoPath);

    return _modelItem;
  }

  private static string ElementDescriptor(ModelItem modelItem)
  {
    var simpleType = modelItem
      .GetType()
      .ToString()
      .Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault();
    return string.IsNullOrEmpty(modelItem.ClassDisplayName)
      ? $"{simpleType}"
      : $"{simpleType} {modelItem.ClassDisplayName}";
  }

  public string Descriptor()
  {
    return _modelItem == null ? null : ElementDescriptor(_modelItem);
  }

  public static IEnumerable<Base> BuildNestedObjectHierarchy(Dictionary<string, Base> convertedDictionary)
  {
    var dictionary = convertedDictionary
      .Where(e => e.Value != null)
      .ToDictionary(
        entry => entry.Key,
        entry =>
          new Tuple<Base, string>(
            entry.Value,
            !entry.Key.Equals(Constants.RootNodePseudoId)
              ? entry.Key.Contains('-')
                ? entry.Key.Substring(0, entry.Key.LastIndexOf('-'))
                : Constants.RootNodePseudoId
              : null
          )
      );
    var parentGroups = dictionary.Values.Where(x => !string.IsNullOrEmpty(x.Item2)).GroupBy(x => x.Item2);

    var rootSet = new HashSet<Base>();

    foreach (var group in parentGroups)
    {
      if (!dictionary.TryGetValue(group.Key, out var parentTuple))
      {
        foreach ((Base item, string parentId) in group)
        {
          if (item is Collection collection)
          {
            var childEntries = dictionary.Values.Where(x => x.Item2 == parentId).ToList();
            collection.elements = childEntries.Select(x => x.Item1).ToList();

            foreach (var childEntry in childEntries.Where(x => !(x.Item1 is Collection)))
            {
              dictionary.Remove((string)childEntry.Item1["applicationId"]);
            }
          }

          rootSet.Add(item);
        }

        continue;
      }

      var parent = parentTuple.Item1;

      var childList = new List<Base>();

      foreach (var child in group)
      {
        if (child.Item1 is not Collection)
        {
          dictionary.Remove(child.Item2);
        }

        childList.Add(child.Item1);
      }

      ((Collection)parent).elements = childList;
    }

    var entriesToRemove = dictionary.Values
      .Where(x => !string.IsNullOrEmpty(x.Item2) && dictionary.ContainsKey(x.Item2))
      .ToList();

    foreach (var entryToRemove in entriesToRemove)
    {
      dictionary.Remove((string)entryToRemove.Item1["applicationId"]);
    }

    rootSet.UnionWith(dictionary.Values.Where(x => string.IsNullOrEmpty(x.Item2)).Select(x => x.Item1));
    return rootSet.ToList();
  }
}
