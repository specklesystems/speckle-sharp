using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using Speckle.Core.Models;

namespace Speckle.ConnectorNavisworks.Other;

public class Element
{
  private ModelItem _modelItem;

  /// <summary>
  /// Initializes a new instance of the Element class with a specified pseudoId and optionally a ModelItem.
  /// </summary>
  /// <param name="pseudoId">The pseudoId used to create the Element instance.</param>
  /// <param name="modelItem">Optional ModelItem used to create the Element instance. Default is null.</param>
  private Element(string pseudoId, ModelItem modelItem = null)
  {
    PseudoId = pseudoId;
    _modelItem = modelItem;
  }

  private Element(string pseudoId)
  {
    PseudoId = pseudoId;
  }

  public Element() { }

  public ModelItem ModelItem => Resolve();

  public string PseudoId { get; private set; }

  /// <summary>
  /// Creates a new Element instance using a given pseudoId.
  /// </summary>
  /// <param name="pseudoId">The pseudoId used to create the Element instance.</param>
  /// <returns>A new Element instance with its pseudoId set.</returns>
  public static Element GetElement(string pseudoId)
  {
    return new Element(pseudoId);
  }

  /// <summary>
  /// Creates a new Element instance using a given ModelItem.
  /// </summary>
  /// <param name="modelItem">The ModelItem used to create the Element instance.</param>
  /// <returns>A new Element instance with its PseudoId and _modelItem field set.</returns>
  public Element GetElement(ModelItem modelItem)
  {
    return new Element(GetPseudoId(modelItem), modelItem);
  }

  /// <summary>
  /// Gets the PseudoId for the given ModelItem.
  /// </summary>
  /// <param name="modelItem">The ModelItem for which to get the PseudoId.</param>
  /// <returns>The PseudoId of the given ModelItem. If the PseudoId is not set, it is calculated and returned.</returns>
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

  /// <summary>
  /// Resolves a ModelItem from a PseudoId.
  /// </summary>
  /// <returns>A ModelItem that corresponds to the PseudoId, or null if the PseudoId could not be resolved.</returns>
  private ModelItem Resolve()
  {
    if (_modelItem != null)
      return _modelItem;

    if (PseudoId == Constants.RootNodePseudoId)
      return Application.ActiveDocument.Models.RootItems.First;

    if (PseudoId != null)
    {
      int[] pathArray;

      try
      {
        pathArray = ParsePseudoIdToPathArray(PseudoId);
      }
      catch (ArgumentException)
      {
        return null;
      }

      var oneBasedArray = ConvertTo1BasedArray(pathArray);
      var protoPath = CreateProtoPath(oneBasedArray);

      _modelItem = ComApiBridge.ToModelItem(protoPath);
    }

    return _modelItem;
  }

  /// <summary>
  /// Parses a PseudoId into a path array.
  /// </summary>
  /// <param name="pseudoId">The PseudoId to parse.</param>
  /// <returns>An array of integers representing the path.</returns>
  /// <exception cref="ArgumentException">Thrown when the PseudoId is malformed.</exception>
  private int[] ParsePseudoIdToPathArray(string pseudoId)
  {
    return pseudoId
      .Split('-')
      .Select(x =>
      {
        if (int.TryParse(x, out var value))
          return value;

        throw new ArgumentException("malformed path pseudoId");
      })
      .ToArray();
  }

  /// <summary>
  /// Converts a zero-based integer array into a one-based array.
  /// </summary>
  /// <param name="pathArray">The zero-based integer array to convert.</param>
  /// <returns>A one-based array with the same elements as the input array.</returns>
  private Array ConvertTo1BasedArray(int[] pathArray)
  {
    var oneBasedArray = Array.CreateInstance(typeof(int), new[] { pathArray.Length }, new[] { 1 });
    Array.Copy(pathArray, 0, oneBasedArray, 1, pathArray.Length);
    return oneBasedArray;
  }

  /// <summary>
  /// Creates a protoPath from a one-based array.
  /// </summary>
  /// <param name="oneBasedArray">The one-based array to use for creating the protoPath.</param>
  /// <returns>A protoPath that corresponds to the input array.</returns>
  private InwOaPath CreateProtoPath(Array oneBasedArray)
  {
    var oState = ComApiBridge.State;
    var protoPath = (InwOaPath)oState.ObjectFactory(nwEObjectType.eObjectType_nwOaPath);
    protoPath.ArrayData = oneBasedArray;
    return protoPath;
  }

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
      .Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries)
      .LastOrDefault();
    return string.IsNullOrEmpty(modelItem.ClassDisplayName)
      ? $"{simpleType}"
      : $"{simpleType} {modelItem.ClassDisplayName}";
  }

  /// <summary>
  /// Generates a descriptor for the current model item.
  /// </summary>
  /// <returns>A descriptor for the current model item, or null if no model item is set.</returns>
  public string Descriptor()
  {
    return _modelItem == null ? null : ElementDescriptor(_modelItem);
  }

  /// <summary>
  /// Builds a nested object hierarchy from a dictionary of flat key-value pairs.
  /// </summary>
  /// <param name="converted"></param>
  /// <param name="streamState"></param>
  /// <param name="convertedDictionary">The input dictionary to be converted into a hierarchical structure.</param>
  /// <returns>An IEnumerable of root nodes representing the hierarchical structure.</returns>
  public static IEnumerable<Base> BuildNestedObjectHierarchy(
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> converted,
    StreamState streamState
  )
  {
    var convertedDictionary = converted.ToDictionary(x => x.Key.PseudoId, x => (x.Value.Item2, x.Key));

    // This dictionary is for looking up parents quickly
    Dictionary<string, Base> lookupDictionary = new();

    // This dictionary will hold potential root nodes until we confirm they are roots
    Dictionary<string, Base> potentialRootNodes = new();

    // First pass: Create lookup dictionary and identify potential root nodes
    foreach (var pair in convertedDictionary)
    {
      var element = pair.Value.Item2;
      var pseudoId = element.PseudoId;
      var baseNode = pair.Value.Item1;
      var modelItem = element.ModelItem;
      var type = baseNode?.GetType().Name;

      if (baseNode == null)
        continue;

      // Geometry Nodes can add all the properties to the FirstObject classification - this will help with the selection logic
      if (
        streamState.Settings.Find(x => x.Slug == "coalesce-data") is CheckBoxSetting { IsChecked: true }
        && type == "GeometryNode"
      )
        AddPropertyStackToGeometryNode(converted, modelItem, baseNode);

      string[] parts = pseudoId.Split('-');
      string parentKey = string.Join("-", parts.Take(parts.Length - 1));

      lookupDictionary.Add(pseudoId, baseNode);

      if (!lookupDictionary.ContainsKey(parentKey))
        potentialRootNodes.Add(pseudoId, baseNode);
    }

    // Second pass: Attach child nodes to their parents, and confirm root nodes
    foreach (var pair in lookupDictionary)
    {
      string key = pair.Key;
      Base value = pair.Value;

      string[] parts = key.Split('-');
      string parentKey = string.Join("-", parts.Take(parts.Length - 1));

      if (!lookupDictionary.TryGetValue(parentKey, out Base value1))
        continue;
      if (value1 is Collection parent)
      {
        parent.elements ??= new List<Base>();
        if (value != null)
          parent.elements.Add(value);
      }

      // This node has a parent, so it's not a root node
      potentialRootNodes.Remove(key);
    }

    List<Base> rootNodes = potentialRootNodes.Values.ToList();

    foreach (var rootNode in rootNodes.Where(rootNode => rootNode != null))
      PruneEmptyCollections(rootNode);

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
      return;
    if (collection.elements == null)
      return;

    for (int i = collection.elements.Count - 1; i >= 0; i--)
    {
      PruneEmptyCollections(collection.elements[i]);

      if (
        collection.elements[i] is Collection childCollection
        && (childCollection.elements == null || childCollection.elements.Count == 0)
      )
        collection.elements.RemoveAt(i);
    }

    if (collection.elements.Count == 0)
      collection.elements = null;
  }
}
