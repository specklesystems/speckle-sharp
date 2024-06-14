using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.DocumentParts;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks.Other;

public class Element
{
  private ModelItem _modelItem;

  private string _indexPath;

  public string IndexPath
  {
    get
    {
      if (_indexPath == null && _modelItem != null)
      {
        _indexPath = ResolveModelItemToIndexPath(_modelItem);
      }

      return _indexPath;
    }
  }

  public ModelItem ModelItem
  {
    get
    {
      if (_modelItem == null && _indexPath != null)
      {
        _modelItem = ResolveIndexPath(_indexPath);
      }

      return _modelItem;
    }
  }

  public Element(string indexPath)
  {
    _indexPath = indexPath;
  }

  public Element(ModelItem modelItem)
  {
    _modelItem = modelItem;
  }

  private static readonly string[] s_separator = { "." };

  /// <summary>
  /// Generates a descriptor for the given model item.
  /// </summary>
  /// <param name="modelItem">The model item to generate a descriptor for.</param>
  /// <returns>A descriptor for the model item.</returns>
  private static string ElementDescriptor(ModelItem modelItem)
  {
    var simpleType = modelItem
      .GetType()
      .ToString()
      .Split(s_separator, StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault();
    return string.IsNullOrEmpty(modelItem.ClassDisplayName)
      ? $"{simpleType}"
      : $"{simpleType} {modelItem.ClassDisplayName}";
  }

  /// <summary>
  /// Generates a descriptor for the current model item.
  /// </summary>
  /// <returns>A descriptor for the current model item, or null if no model item is set.</returns>
  public string Descriptor() => _modelItem == null ? null : ElementDescriptor(_modelItem);

  /// <summary>
  /// Builds a nested object hierarchy from a dictionary of flat key-value pairs.
  /// </summary>
  /// <param name="converted"></param>
  /// <param name="streamState"></param>
  /// <returns>An IEnumerable of root nodes representing the hierarchical structure.</returns>
  public static IEnumerable<Base> BuildNestedObjectHierarchy(
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> converted,
    StreamState streamState
  )
  {
    var convertedDictionary = converted.ToDictionary(x => x.Key.IndexPath, x => (x.Value.Item2, x.Key));

    // This dictionary is for looking up parents quickly
    Dictionary<string, Base> lookupDictionary = new();

    // This dictionary will hold potential root nodes until we confirm they are roots
    Dictionary<string, Base> potentialRootNodes = new();

    // First pass: Create lookup dictionary and identify potential root nodes
    foreach (var pair in convertedDictionary)
    {
      var element = pair.Value.Key;
      var indexPath = element.IndexPath;
      var baseNode = pair.Value.Item1;
      var modelItem = element.ModelItem;
      var type = baseNode?.GetType().Name;

      if (baseNode == null)
      {
        continue;
      }

      // Geometry Nodes can add all the properties to the FirstObject classification - this will help with the selection logic
      if (
        streamState.Settings.Find(x => x.Slug == "coalesce-data") is CheckBoxSetting { IsChecked: true }
        && type == "GeometryNode"
      )
      {
        AddPropertyStackToGeometryNode(converted, modelItem, baseNode);
      }

      string[] parts = indexPath.Split('-');
      string parentKey = string.Join("-", parts.Take(parts.Length - 1));

      lookupDictionary.Add(indexPath, baseNode);

      if (!lookupDictionary.ContainsKey(parentKey))
      {
        potentialRootNodes.Add(indexPath, baseNode);
      }
    }

    // Second pass: Attach child nodes to their parents, and confirm root nodes
    foreach (var pair in lookupDictionary)
    {
      string key = pair.Key;
      Base value = pair.Value;

      string[] parts = key.Split('-');
      string parentKey = string.Join("-", parts.Take(parts.Length - 1));

      if (!lookupDictionary.TryGetValue(parentKey, out Base value1))
      {
        continue;
      }

      if (value1 is Collection parent)
      {
        parent.elements ??= new List<Base>();
        if (value != null)
        {
          parent.elements.Add(value);
        }
      }

      // This node has a parent, so it's not a root node
      potentialRootNodes.Remove(key);
    }

    List<Base> rootNodes = potentialRootNodes.Values.ToList();

    foreach (var rootNode in rootNodes.Where(rootNode => rootNode != null))
    {
      PruneEmptyCollections(rootNode);
    }

    rootNodes.RemoveAll(node => node is Collection { elements: null });

    return rootNodes;
  }

  /// <summary>
  /// Adds the property stack to a geometry node based on converted elements and model item.
  /// </summary>
  /// <param name="converted">The converted dictionary of elements.</param>
  /// <param name="modelItem">The model item to process.</param>
  /// <param name="baseNode">The base node to update with the property stack.</param>
  private static void AddPropertyStackToGeometryNode(
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> converted,
    ModelItem modelItem,
    DynamicBase baseNode
  )
  {
    var firstObjectAncestor = modelItem.FindFirstObjectAncestor();
    var ancestors = modelItem.Ancestors;
    var trimmedAncestors = ancestors.TakeWhile(ancestor => ancestor != firstObjectAncestor).Append(firstObjectAncestor);

    var propertyStack = trimmedAncestors
      .Select(item => converted.FirstOrDefault(keyValuePair => Equals(keyValuePair.Key.ModelItem, item)))
      .Select(kVp => kVp.Value.Item2["properties"] as Base)
      .SelectMany(
        propertySet => propertySet?.GetMembers().Where(member => member.Value is Base),
        (_, propertyCategory) =>
          new { Category = propertyCategory.Key, Properties = ((Base)propertyCategory.Value).GetMembers() }
      )
      .SelectMany(
        categoryProperties => categoryProperties.Properties,
        (categoryProperties, property) =>
          new { ConcatenatedKey = $"{categoryProperties.Category}--{property.Key}", property.Value }
      )
      .Where(property => property.Value != null && !string.IsNullOrEmpty(property.Value.ToString()))
      .GroupBy(property => property.ConcatenatedKey)
      .Where(group => group.Select(item => item.Value).Distinct().Count() == 1)
      .ToDictionary(group => group.Key, group => group.First().Value)
      .Select(
        kVp =>
          new
          {
            Category = kVp.Key.Substring(0, kVp.Key.IndexOf("--", StringComparison.Ordinal)),
            Property = kVp.Key.Substring(kVp.Key.IndexOf("--", StringComparison.Ordinal) + 2),
            kVp.Value
          }
      )
      .Where(item => item.Category != "Internal")
      .GroupBy(item => item.Category)
      .ToDictionary(group => group.Key, group => group.ToDictionary(item => item.Property, item => item.Value));

    var propertiesBase = (Base)baseNode["properties"];

    var baseProperties = propertiesBase.GetMembers().Where(item => item.Value is Base).ToList();

    foreach (var baseProperty in baseProperties)
    {
      var baseSubProperties = ((Base)baseProperty.Value).GetMembers().ToList();

      if (!propertyStack.TryGetValue(baseProperty.Key, out var stackProperty))
      {
        stackProperty = baseSubProperties.ToDictionary(item => item.Key, item => item.Value);
        propertyStack.Add(baseProperty.Key, stackProperty);
      }
      else
      {
        var stackPropertySet = new HashSet<string>(stackProperty.Keys);

        foreach (var subProperty in baseSubProperties)
        {
          if (stackPropertySet.Contains(subProperty.Key))
          {
            stackProperty[subProperty.Key] = subProperty.Value;
          }
          else
          {
            stackProperty.Add(subProperty.Key, subProperty.Value);
          }
        }
      }
    }

    foreach (var stackProperty in propertyStack)
    {
      if (propertiesBase[stackProperty.Key] is Base basePropertyCategory)
      {
        foreach (var kvp in stackProperty.Value)
        {
          basePropertyCategory[kvp.Key] = kvp.Value;
        }
      }
      else
      {
        var newPropertyCategory = new Base();

        foreach (var kvp in stackProperty.Value)
        {
          newPropertyCategory[kvp.Key] = kvp.Value;
        }

        propertiesBase[stackProperty.Key] = newPropertyCategory;
      }
    }

    // baseNode["property-stack"] = propertyStack;
  }

  /// <summary>
  /// Recursively prunes empty collections from the given node and its descendants.
  /// </summary>
  /// <param name="node">The node to start pruning from.</param>
  private static void PruneEmptyCollections(IDynamicMetaObjectProvider node)
  {
    if (node is not Collection collection)
    {
      return;
    }

    if (collection.elements == null)
    {
      return;
    }

    for (int i = collection.elements.Count - 1; i >= 0; i--)
    {
      PruneEmptyCollections(collection.elements[i]);

      if (
        collection.elements[i] is Collection childCollection
        && (childCollection.elements == null || childCollection.elements.Count == 0)
      )
      {
        collection.elements.RemoveAt(i);
      }
    }

    if (collection.elements.Count == 0)
    {
      collection.elements = null;
    }
  }

  public static ModelItem ResolveIndexPath(string indexPath)
  {
    var indexPathParts = indexPath.Split('-');

    // assign the first part of indexPathParts to modelIndex and parse it to int, the second part to pathId string
    ModelItemPathId modelItemPathId = new() { ModelIndex = int.Parse(indexPathParts[0]), PathId = indexPathParts[1] };

    var modelItem = Application.ActiveDocument.Models.ResolvePathId(modelItemPathId);
    return modelItem;
  }

  public static string ResolveModelItemToIndexPath(ModelItem modelItem)
  {
    var modelItemPathId = Application.ActiveDocument.Models.CreatePathId(modelItem);
    return $"{modelItemPathId.ModelIndex}-{modelItemPathId.PathId}";
  }
}
