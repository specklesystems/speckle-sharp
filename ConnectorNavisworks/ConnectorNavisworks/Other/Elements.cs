using System;
using System.Collections.Concurrent;
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
  private const char SEPARATOR = '/';

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

  private static readonly string[] s_separator = { SEPARATOR.ToString() };

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
  public static IEnumerable<Base> BuildNestedObjectHierarchyInParallel(
    Dictionary<Element, Tuple<Constants.ConversionState, Base>> converted,
    StreamState streamState,
    ProgressInvoker progressBar
  )
  {
    var convertedDictionary = converted.ToDictionary(x => x.Key.IndexPath, x => (x.Value.Item2, x.Key));

    ConcurrentDictionary<string, Base> lookupDictionary = new();
    ConcurrentDictionary<string, Base> potentialRootNodes = new();

    int totalCount = convertedDictionary.Count;
    const int DEFAULT_UPDATE_INTERVAL = 1000;

    List<Base> rootNodes = new(); // Initialize rootNodes here

    try
    {
      // First pass: Populate lookup dictionary and identify potential root nodes
      ExecuteWithProgress(
        totalCount,
        progressBar,
        "Identifying roots",
        i =>
        {
          var pair = convertedDictionary.ElementAt(i);
          var element = pair.Value.Key;
          var indexPath = element.IndexPath;
          var baseNode = pair.Value.Item1;
          var modelItem = element.ModelItem;
          var type = baseNode?.GetType().Name;

          if (baseNode == null)
          {
            return;
          }

          if (
            streamState.Settings.Find(x => x.Slug == "coalesce-data") is CheckBoxSetting { IsChecked: true }
            && type == "GeometryNode"
          )
          {
            AddPropertyStackToGeometryNode(converted, modelItem, baseNode);
          }

          string[] parts = indexPath.Split(SEPARATOR);
          string parentKey = string.Join(SEPARATOR.ToString(), parts.Take(parts.Length - 1));

          lookupDictionary.TryAdd(indexPath, baseNode);

          if (!lookupDictionary.ContainsKey(parentKey))
          {
            potentialRootNodes.TryAdd(indexPath, baseNode);
          }
        }
      );

      // Second pass: Attach child nodes to parents and confirm root nodes
      ExecuteWithProgress(
        lookupDictionary.Count,
        progressBar,
        "Reuniting children with parents",
        i =>
        {
          var pair = lookupDictionary.ElementAt(i);
          string key = pair.Key;
          Base value = pair.Value;

          string[] parts = key.Split(SEPARATOR);
          string parentKey = string.Join(SEPARATOR.ToString(), parts.Take(parts.Length - 1));

          if (!lookupDictionary.TryGetValue(parentKey, out Base parentValue) || parentValue is not Collection parent)
          {
            return;
          }

          parent.elements.Add(value);

          potentialRootNodes.TryRemove(key, out _);
        }
      );

      rootNodes = potentialRootNodes.Values.ToList();

      // Prune empty collections
      ExecuteWithProgress(
        rootNodes.Count,
        progressBar,
        "Recycling empties",
        i =>
        {
          var rootNode = rootNodes[i];
          if (rootNode != null)
          {
            PruneEmptyCollections(rootNode);
          }
        }
      );

      rootNodes.RemoveAll(node => node is Collection { elements: null });
    }
    catch (OperationCanceledException)
    {
      // Handle cancellation if needed
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("An error occurred during the operation.", ex);
    }

    return rootNodes;
  }

  private static void ExecuteWithProgress(
    int totalCount,
    ProgressInvoker progressBar,
    string operationName,
    Action<int> action
  )
  {
    int progressCounter = 0;
    const int DEFAULT_UPDATE_INTERVAL = 1000;

    progressBar.BeginSubOperation(0.2, operationName);
    progressBar.Update(0);

    for (int i = 0; i < totalCount; i++)
    {
      action(i);

      progressCounter++;
      if (progressCounter % DEFAULT_UPDATE_INTERVAL != 0 && progressCounter != totalCount)
      {
        continue;
      }

      double progressValue = Math.Min((double)progressCounter / totalCount, 1.0);
      progressBar.Update(progressValue);
    }

    progressBar.EndSubOperation();
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
    if (modelItem == null || baseNode == null || converted == null)
    {
      throw new ArgumentNullException(
        modelItem == null
          ? nameof(modelItem)
          : baseNode == null
            ? nameof(baseNode)
            : nameof(converted)
      );
    }

    var firstObjectAncestor =
      modelItem.FindFirstObjectAncestor() ?? throw new InvalidOperationException("firstObjectAncestor is null.");
    var ancestors = modelItem.Ancestors ?? throw new InvalidOperationException("ancestors is null.");
    var trimmedAncestors = ancestors.TakeWhile(ancestor => ancestor != firstObjectAncestor).Append(firstObjectAncestor);

    var filtered = trimmedAncestors
      .Select(item => converted.FirstOrDefault(keyValuePair => Equals(keyValuePair.Key.ModelItem, item)))
      .Where(kVp => kVp.Key != null) // Filter out null keys
      .Select(kVp => kVp.Value.Item2["properties"] as Base)
      .Where(propertySet => propertySet != null); // Filter out null property sets

    var categoryProperties = filtered.SelectMany(
      propertySet => propertySet.GetMembers().Where(member => member.Value is Base),
      (_, propertyCategory) =>
        new { Category = propertyCategory.Key, Properties = ((Base)propertyCategory.Value).GetMembers() }
    );

    var properties = categoryProperties
      .SelectMany(
        cp => cp.Properties,
        (cp, property) => new { ConcatenatedKey = $"{cp.Category}--{property.Key}", property.Value }
      )
      .Where(property => property.Value != null && !string.IsNullOrEmpty(property.Value.ToString()));

    var groupedProperties = properties
      .GroupBy(property => property.ConcatenatedKey)
      .ToDictionary(group => group.Key, group => group.Select(item => item.Value).Last());

    var formattedProperties = groupedProperties
      .Select(kVp => new
      {
        Category = kVp.Key.Substring(0, kVp.Key.IndexOf("--", StringComparison.Ordinal)),
        Property = kVp.Key.Substring(kVp.Key.IndexOf("--", StringComparison.Ordinal) + 2),
        kVp.Value
      })
      .Where(item => item.Category != "Internal");

    var propertyStack = formattedProperties
      .GroupBy(item => item.Category)
      .ToDictionary(group => group.Key, group => group.ToDictionary(item => item.Property, item => item.Value));

    if (baseNode["properties"] is not Base propertiesBase)
    {
      propertiesBase = new Base();
      baseNode["properties"] = propertiesBase;
    }

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

    if (collection.elements.Count == 0)
    {
      return;
    }

    for (int i = collection.elements.Count - 1; i >= 0; i--)
    {
      PruneEmptyCollections(collection.elements[i]);

      if (collection.elements[i] is Collection { elements.Count: 0 })
      {
        collection.elements.RemoveAt(i);
      }
    }

    if (collection.elements.Count == 0)
    {
      collection.elements = null!;
    }
  }

  public static ModelItem ResolveIndexPath(string indexPath)
  {
    var indexPathParts = indexPath.Split(SEPARATOR);

    var modelIndex = int.Parse(indexPathParts[0]);
    var pathId = string.Join(SEPARATOR.ToString(), indexPathParts.Skip(1));

    // assign the first part of indexPathParts to modelIndex and parse it to int, the second part to pathId string
    ModelItemPathId modelItemPathId = new() { ModelIndex = modelIndex, PathId = pathId };

    var modelItem = Application.ActiveDocument.Models.ResolvePathId(modelItemPathId);
    return modelItem;
  }

  public static string ResolveModelItemToIndexPath(ModelItem modelItem)
  {
    var modelItemPathId = Application.ActiveDocument.Models.CreatePathId(modelItem);

    return modelItemPathId.PathId == "a"
      ? $"{modelItemPathId.ModelIndex}"
      : $"{modelItemPathId.ModelIndex}{SEPARATOR}{modelItemPathId.PathId}";
  }

  /// <summary>
  ///   Checks is the Element is hidden or if any of its ancestors is hidden
  /// </summary>
  /// <param name="element"></param>
  /// <returns></returns>
  internal static bool IsElementVisible(ModelItem element) =>
    // Hidden status is stored at the earliest node in the hierarchy
    // All the tree path nodes need to not be Hidden
    element.AncestorsAndSelf.All(x => x.IsHidden != true);
}
