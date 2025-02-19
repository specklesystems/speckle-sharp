using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Gui;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;

namespace Speckle.ConnectorNavisworks.Other;

public class SelectionHandler
{
  private readonly ISelectionFilter _filter;
  private readonly bool _fullTreeSetting;
  private readonly ProgressViewModel _progressViewModel;
  private readonly HashSet<ModelItem> _uniqueModelItems;
  private int _descendantProgress;
  private HashSet<ModelItem> _visited;
  internal ProgressInvoker ProgressBar;
  private readonly bool _coalesceData;

  /// <summary>
  /// Initializes a new instance of the SelectionHandler class with the specified StreamState and ProgressViewModel.
  /// </summary>
  /// <param name="state">The StreamState object containing the filter and settings.</param>
  /// <param name="progressViewModel">The ProgressViewModel object for tracking progress.</param>
  public SelectionHandler(StreamState state, ProgressViewModel progressViewModel)
  {
    _progressViewModel = progressViewModel;
    _filter = state.Filter;
    _uniqueModelItems = new HashSet<ModelItem>();
    _fullTreeSetting =
      state.Settings.OfType<CheckBoxSetting>().FirstOrDefault(x => x.Slug == "full-tree")?.IsChecked ?? false;
    _coalesceData =
      state.Settings.OfType<CheckBoxSetting>().FirstOrDefault(x => x.Slug == "coalesce-data")?.IsChecked ?? false;
  }

  public int Count => _uniqueModelItems.Count;

  public IEnumerable<ModelItem> ModelItems => _uniqueModelItems.ToList().AsReadOnly();

  /// <summary>
  /// Retrieves objects based on the selected filter type.
  /// </summary>
  public void GetFromFilter()
  {
    switch (_filter.Slug)
    {
      case FilterTypes.MANUAL:
        _uniqueModelItems.AddRange(GetObjectsFromSelection());
        break;

      case FilterTypes.SETS:
        _uniqueModelItems.AddRange(GetObjectsFromSavedSets());
        break;

      case FilterTypes.VIEWS:
        _uniqueModelItems.AddRange(GetObjectsFromSavedViewpoint());
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(_filter.Slug), _filter.Slug, "Unrecognized filter type");
    }
  }

  /// <summary>
  /// Retrieves the model items from the selection.
  /// </summary>
  private HashSet<ModelItem> GetObjectsFromSelection()
  {
    _uniqueModelItems.Clear();

    // Selections are modelItem pseudo-ids.
    var selection = _filter.Selection;

    ProgressLooper(
      "Rolling up the sleeves... Time to handpick your favourite data items!",
      (index) =>
      {
        if (index >= selection.Count)
        {
          return false;
        }

        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();
        _uniqueModelItems.Add(Element.ResolveIndexPath(selection[index]));

        return true;
      },
      0.05, // Fraction of remaining time
      totalCount: selection.Count // Pass the total count if known, else pass null
    );

    return _uniqueModelItems;
  }

  /// <summary>
  /// Retrieves the model items from the saved viewpoint.
  /// </summary>
  private HashSet<ModelItem> GetObjectsFromSavedViewpoint()
  {
    _uniqueModelItems.Clear();

    // Get the selection from the filter
    var selection = _filter.Selection.FirstOrDefault();
    if (string.IsNullOrEmpty(selection))
    {
      return new HashSet<ModelItem>();
    }

    // Resolve the saved viewpoint based on the selection
    var success = false;

    new Invoker().Invoke(() =>
    {
      var savedViewpoint = ResolveSavedViewpoint(selection);
      if (savedViewpoint != null && !savedViewpoint.ContainsVisibilityOverrides)
      {
        return;
      }

      Application.ActiveDocument.SavedViewpoints.CurrentSavedViewpoint = savedViewpoint;
      success = true;
    });

    if (!success)
    {
      return new HashSet<ModelItem>();
    }

    var models = Application.ActiveDocument.Models;
    Application.ActiveDocument.CurrentSelection.Clear();

    // Use ProgressLooper to handle the looping and progress updates
    ProgressLooper(
      "Checking the Canvas... Looking Closely!",
      (index) =>
      {
        if (index >= models.Count)
        {
          return false;
        }

        var model = models[index];
        var rootItem = model.RootItem;
        if (!rootItem.IsHidden)
        {
          _uniqueModelItems.Add(rootItem);
        }

        return true;
      },
      0.05, // Fraction of remaining time
      models.Count // Pass the total count if known, else pass null
    );

    return _uniqueModelItems;
  }

  /// <summary>
  /// Resolves the SavedViewpoint based on the provided saved view reference.
  /// </summary>
  /// <param name="savedViewReference">The saved view reference to resolve.</param>
  /// <returns>The resolved SavedViewpoint.</returns>
  public SavedViewpoint ResolveSavedViewpoint(string savedViewReference)
  {
    // Get a flattened list of viewpoints and their references
    var flattenedViewpointList = Application
      .ActiveDocument.SavedViewpoints.RootItem.Children.Select(GetViews)
      .Where(x => x != null)
      .SelectMany(node => node.Flatten())
      .Select(node => new { node.Reference, node.Guid })
      .ToList();

    // Find a match based on the saved view reference
    var viewPointMatch = flattenedViewpointList.FirstOrDefault(node =>
      node.Guid.ToString() == savedViewReference || node.Reference == savedViewReference
    );

    if (viewPointMatch != null)
    {
      return ResolveSavedViewpointMatch(savedViewReference);
    }

    {
      foreach (var node in flattenedViewpointList)
      {
        if (node.Guid.ToString() != savedViewReference)
        {
          if (node.Reference != savedViewReference)
          {
            continue;
          }
        }

        viewPointMatch = node;
        break;
      }
    }

    // If no match is found, return null; otherwise, resolve the SavedViewpoint
    return viewPointMatch == null ? null : ResolveSavedViewpointMatch(savedViewReference);
  }

  /// <summary>
  /// Resolves the SavedViewpoint based on the provided viewpoint match and saved view reference.
  /// </summary>
  /// <param name="savedViewReference">The saved view reference to resolve.</param>
  /// <returns>The resolved SavedViewpoint.</returns>
  private SavedViewpoint ResolveSavedViewpointMatch(string savedViewReference)
  {
    if (Guid.TryParse(savedViewReference, out var guid))
    {
      // Even though we may have already got a match, that could be to a generic Guid from earlier versions of Navisworks
      if (savedViewReference != Guid.Empty.ToString())
      {
        return (SavedViewpoint)Application.ActiveDocument.SavedViewpoints.ResolveGuid(guid);
      }
    }

    var savedRef = new SavedItemReference("LcOpSavedViewsElement", savedViewReference);

    var invoker = new Invoker();

    var resolvedReference = invoker.Invoke(Application.ActiveDocument.ResolveReference, savedRef) as SavedViewpoint;

    // var resolvedReference = Application.ActiveDocument.ResolveReference(savedRef);
    return resolvedReference;
  }

  /// <summary>
  /// Retrieves the TreeNode representing views for a given SavedItem.
  /// </summary>
  /// <param name="savedItem">The SavedItem for which to retrieve the views.</param>
  /// <returns>The TreeNode representing the views for the given SavedItem.</returns>
  private TreeNode GetViews(SavedItem savedItem)
  {
    var invoker = new Invoker();
    // Create a reference to the SavedItem
    SavedItemReference reference =
      invoker.Invoke(Application.ActiveDocument.SavedViewpoints.CreateReference, savedItem) as SavedItemReference;

    // Create a new TreeNode with properties based on the SavedItem
    var treeNode = new TreeNode
    {
      DisplayName = savedItem.DisplayName,
      Guid = savedItem.Guid,
      IndexWith = nameof(TreeNode.Reference),
      // Rather than version check Navisworks host application we feature check
      // to see if Guid is set correctly on viewpoints.
      Reference =
        savedItem.Guid.ToString() == Guid.Empty.ToString() ? reference?.SavedItemId : savedItem.Guid.ToString()
    };

    // Handle different cases based on whether the SavedItem is a group or not
    switch (savedItem)
    {
      case SavedViewpoint { ContainsVisibilityOverrides: false }:
        // TODO: Determine whether to return null or an empty TreeNode or based on current visibility. This is another don't send everything safeguard.
        return null;
      case GroupItem groupItem:
        foreach (var childItem in groupItem.Children)
        {
          treeNode.IsEnabled = false;
          treeNode.Elements.Add(GetViews(childItem));
        }
        break;
      default:
        // This case is intentionally left empty as all SDK object scenarios are covered above.
        // and will fall throw with the treeNode for a SavedViewpoint that is not a group and has visibility overrides.
        break;
    }

    // Return the treeNode
    return treeNode;
  }

  /// <summary>
  /// Retrieves the model items from the saved sets.
  /// </summary>
  private HashSet<ModelItem> GetObjectsFromSavedSets()
  {
    _uniqueModelItems.Clear();

    // Saved Sets filter stores Guids of the selection sets. This can be converted to ModelItem pseudoIds
    var selections = _filter.Selection.Select(guid => new Guid(guid)).ToList();

    // Resolve the saved items and extract the inner selection sets when folder items are encountered
    var savedItems = selections
      .Select(guid => Application.ActiveDocument.SelectionSets.ResolveGuid(guid))
      .SelectMany(ExtractSelectionSets)
      .ToList();

    foreach (var item in savedItems)
    {
      if (item.HasExplicitModelItems)
      {
        // This is for saved selections. If the models are as were when selection was made, then the static results are all valid.
        _uniqueModelItems.AddRange(item.ExplicitModelItems);
      }
      else if (item.HasSearch) // This is for saved searches. The results are dynamic and need to be resolved.
      {
        // This is for saved searches. The results are dynamic and need to be resolved.
        var foundModelItems = item.Search.FindAll(Application.ActiveDocument, false);
        _uniqueModelItems.AddRange(foundModelItems);
      }
    }

    return _uniqueModelItems;
  }

  /// <summary>
  /// Recursively extracts SelectionSet objects from the given item.
  /// </summary>
  /// <param name="selection">The object to extract SelectionSets from. Can be a SelectionSet, FolderItem, or any other object.</param>
  /// <returns>An IEnumerable of SelectionSet objects extracted from the item and its children (if applicable).</returns>
  /// <exception cref="ArgumentNullException">Thrown if the input item is null.</exception>
  static IEnumerable<SelectionSet> ExtractSelectionSets(object selection)
  {
    if (selection == null)
    {
      throw new ArgumentNullException(nameof(selection), "Input item cannot be null.");
    }

    switch (selection)
    {
      case SelectionSet selectionSet:
        yield return selectionSet;
        break;
      case FolderItem folderItem:
        if (folderItem.Children == null)
        {
          yield break;
        }
        foreach (var childItem in folderItem.Children)
        {
          if (childItem == null)
          {
            continue;
          }

          foreach (var extractedSet in ExtractSelectionSets(childItem))
          {
            yield return extractedSet;
          }
        }
        break;
    }
  }

  /// <summary>
  /// Populates the hierarchy by adding ancestor and descendant items to the unique model items.
  /// The unique model items have already been processed to validate that they are not hidden.
  /// </summary>
  public void PopulateHierarchyAndOmitHidden()
  {
    if (_uniqueModelItems == null || _uniqueModelItems.Count == 0)
    {
      return;
    }

    var startNodes = _uniqueModelItems.ToList();

    // Where data is wanted to be coalesced from First Object Ancestor we need to ensure that the relevant parents are added.
    // If the full tree is selected then there is no reason to get the first object ancestor
    if (_coalesceData && !_fullTreeSetting)
    {
      var miniAncestorTreeNodes = startNodes
        .SelectMany(e =>
        {
          ModelItem targetFirstObjectChild = e.Children.FirstOrDefault() ?? e;

          var firstObjectAncestor = targetFirstObjectChild.FindFirstObjectAncestor();

          if (
            firstObjectAncestor == null
            || Equals(e, firstObjectAncestor)
            || _uniqueModelItems.Contains(firstObjectAncestor)
          )
          {
            return new HashSet<ModelItem>();
          }

          var trimmedAncestors = targetFirstObjectChild
            .Ancestors.TakeWhile(ancestor => ancestor != firstObjectAncestor)
            .Append(firstObjectAncestor);

          return trimmedAncestors;
        })
        .Distinct();

      _uniqueModelItems.UnionWith(miniAncestorTreeNodes);
    }

    if (_fullTreeSetting)
    {
      var allAncestors = startNodes.SelectMany(e => e.Ancestors).Distinct().ToList();

      ProgressLooper(
        "Brb, time traveling to find your data's great-grandparents...",
        i =>
        {
          _uniqueModelItems.Add(allAncestors[i]);
          return true;
        },
        0.05,
        allAncestors.Count
      );
    }

    _visited = new HashSet<ModelItem>();
    _descendantProgress = 0;

    HashSet<ModelItem> distinctDescendants = DistinctDescendants(startNodes);
    var allDescendants = distinctDescendants.Count;

    ProgressLooper(
      "Validating descendants...",
      i =>
      {
        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

        TraverseDescendants(
          startNodes[i],
          allDescendants,
          new Progress<double>(value =>
          {
            ProgressBar.Update(value);
          })
        );

        return true;
      },
      0.1,
      startNodes.Count
    );
  }

  private static HashSet<ModelItem> DistinctDescendants(List<ModelItem> startNodes)
  {
    var distinctDescendants = new HashSet<ModelItem>();

    foreach (var node in startNodes)
    {
      var nodeDescendants = node.Descendants.ToList();
      distinctDescendants.UnionWith(nodeDescendants);
    }

    return distinctDescendants;
  }

  /// <summary>
  /// Traverses the descendants of a given model item and updates a progress bar.
  /// </summary>
  /// <param name="startNode">The starting node for traversal.</param>
  /// <param name="totalDescendants">The total number of descendants.</param>
  private void TraverseDescendants(ModelItem startNode, int totalDescendants, IProgress<double> progress)
  {
    var descendantInterval = Math.Max(totalDescendants / 100.0, 1); // Update progress every 1%
    var validDescendants = new HashSet<ModelItem>();

    int updateCounter = 0; // Counter to track when to update the UI
    int lastUpdate = 0; // Track the last update to avoid frequent updates

    Stack<ModelItem> stack = new();
    stack.Push(startNode);

    while (stack.Count > 0)
    {
      if (ProgressBar.IsCanceled)
      {
        _progressViewModel.CancellationTokenSource.Cancel();
      }

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      ModelItem currentNode = stack.Pop();

      // ReSharper disable once CanSimplifySetAddingWithSingleCall
      if (_visited.Contains(currentNode))
      {
        continue;
      }

      _visited.Add(currentNode);

      bool isVisible = IsVisibleCached(currentNode);
      if (!isVisible)
      {
        // If node is hidden, skip processing it and all its descendants
        var descendantsCount = currentNode.Descendants.Count();
        Interlocked.Add(ref _descendantProgress, descendantsCount + 1);
        continue;
      }

      validDescendants.Add(currentNode); // currentNode is visible, process it
      Interlocked.Increment(ref _descendantProgress);

      if (currentNode.Children.Any())
      {
        // Add visible children to the stack

        var childrenToProcess = new ConcurrentBag<ModelItem>();

        Parallel.ForEach(
          currentNode.Children,
          child =>
          {
            if (_visited.Contains(child))
            {
              return;
            }

            if (IsVisibleCached(child))
            {
              childrenToProcess.Add(child);
            }
            else
            {
              // If child is hidden, skip processing it and all its descendants
              int descendantsCount = child.Descendants.Count();
              Interlocked.Add(ref _descendantProgress, descendantsCount + 1);
            }
          }
        );

        foreach (ModelItem child in childrenToProcess)
        {
          stack.Push(child);
        }
      }

      lock (_uniqueModelItems)
      {
        _uniqueModelItems.UnionWith(validDescendants);
      }
      validDescendants.Clear();

      updateCounter++;

      if (!(updateCounter >= descendantInterval) || lastUpdate >= _descendantProgress)
      {
        continue;
      }

      double progressValue = _descendantProgress / (double)totalDescendants;
      progress.Report(progressValue);
      lastUpdate = _descendantProgress;
      updateCounter = 0;
    }
  }

  // Cache to store visibility status of ModelItems
  private readonly Dictionary<ModelItem, bool> _visibilityCache = new();

  /// <summary>
  /// Checks if a ModelItem is visible, with caching to avoid redundant calculations.
  /// </summary>
  /// <param name="item">The ModelItem to check visibility for.</param>
  /// <returns>True if the item is visible, false otherwise.</returns>
  private bool IsVisibleCached(ModelItem item)
  {
    // Check if the result is already in the cache
    if (_visibilityCache.TryGetValue(item, out bool isVisible))
    {
      return isVisible;
    }
    // Calculate visibility if not in cache
    isVisible = CalculateVisibility(item);
    _visibilityCache[item] = isVisible;
    return isVisible;
  }

  /// <summary>
  /// Placeholder for if the default visibility determination logic need augmenting.
  /// </summary>
  /// <param name="item">The ModelItem to check.</param>
  /// <returns>True if visible, false otherwise.</returns>
  private static bool CalculateVisibility(ModelItem item) => !item.IsHidden;

  /// <summary>
  /// Executes a given function while updating a progress bar.
  /// </summary>
  /// <param name="operationName">The name of the operation.</param>
  /// <param name="fn">The function to execute on each iteration.</param>
  /// <param name="fractionOfRemainingTime">The fraction of remaining time for the operation (optional).</param>
  /// <param name="totalCount">The total number of iterations, if known.</param>
  public void ProgressLooper(
    string operationName,
    Func<int, bool> fn,
    double fractionOfRemainingTime = 0,
    int? totalCount = null
  )
  {
    const int DEFAULT_UPDATE_INTERVAL = 1000;
    const double DEFAULT_PROGRESS_INCREMENT = 0.01;

    ProgressBar.BeginSubOperation(fractionOfRemainingTime, operationName);
    ProgressBar.Update(0);

    try
    {
      int i = 0;
      double progress = 0;
      double increment;
      int updateInterval;

      if (totalCount.HasValue)
      {
        increment = 1.0 / totalCount.Value;
        updateInterval = Math.Max(totalCount.Value / 100, 1);
      }
      else
      {
        increment = DEFAULT_PROGRESS_INCREMENT;
        updateInterval = DEFAULT_UPDATE_INTERVAL;
      }

      while (true)
      {
        if (ProgressBar.IsCanceled)
        {
          _progressViewModel.CancellationTokenSource.Cancel();
          break;
        }

        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

        if (!fn(i))
        {
          break;
        }

        var test = fn(i);

        i++;

        if (totalCount.HasValue)
        {
          progress = Math.Min((double)i / totalCount.Value, 1.0);
          if (i % updateInterval == 0 || i == totalCount.Value)
          {
            ProgressBar.Update(progress);
          }

          if (i >= totalCount.Value)
          {
            break;
          }
        }
        else
        {
          if (i % updateInterval != 0)
          {
            continue;
          }

          progress = Math.Min(progress + increment, 1.0);
          ProgressBar.Update(progress);
        }
      }

      ProgressBar.Update(1.0);
    }
    catch (OperationCanceledException)
    {
      // Handle cancellation if needed
    }
    catch (Exception ex)
    {
      throw new InvalidOperationException("An error occurred during the operation.", ex);
    }
    finally
    {
      ProgressBar.EndSubOperation();
    }
  }

  /// <summary>
  /// Omits items that are hidden from the starting list of nodes if they are not visible in the model.
  /// </summary>
  public void ValidateStartNodes() =>
    // Remove any nodes that are descendants of hidden nodes.
    _uniqueModelItems.RemoveWhere(e => e.AncestorsAndSelf.Any(a => a.IsHidden));
}
