using System;
using System.Collections.Generic;
using System.Linq;
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
  public ProgressInvoker ProgressBar;
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
      case FilterTypes.Manual:
        _uniqueModelItems.AddRange(GetObjectsFromSelection());
        break;

      case FilterTypes.Sets:
        _uniqueModelItems.AddRange(GetObjectsFromSavedSets());
        break;

      case FilterTypes.Views:
        _uniqueModelItems.AddRange(GetObjectsFromSavedViewpoint());
        break;
    }
  }

  /// <summary>
  /// Retrieves the model items from the selection.
  /// </summary>
  private IEnumerable<ModelItem> GetObjectsFromSelection()
  {
    _uniqueModelItems.Clear();

    // Selections are modelItem pseudo-ids.
    var selection = _filter.Selection;
    var count = selection.Count;
    var progressIncrement = 1.0 / count != 0 ? count : 1.0;

    // Begin the progress sub-operation for getting objects from selection
    ProgressBar.BeginSubOperation(0.05, "Rolling up the sleeves... Time to handpick your favorite data items!");

    // Iterate over the selection and retrieve the corresponding model items
    for (var i = 0; i < count; i++)
    {
      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();
      ProgressBar.Update(i * progressIncrement);

      var pseudoId = selection[i];
      var element = Element.GetElement(pseudoId);
      _uniqueModelItems.Add(element.ModelItem);
    }

    // End the progress sub-operation
    ProgressBar.EndSubOperation();

    return _uniqueModelItems;
  }

  /// <summary>
  /// Retrieves the model items from the saved viewpoint.
  /// </summary>
  private IEnumerable<ModelItem> GetObjectsFromSavedViewpoint()
  {
    _uniqueModelItems.Clear();

    // Begin the progress sub-operation for getting objects from selection
    ProgressBar.BeginSubOperation(0.05, "Checking the Canvas... Looking Closely!");

    // Get the selection from the filter
    var selection = _filter.Selection.FirstOrDefault();
    if (string.IsNullOrEmpty(selection))
      return Enumerable.Empty<ModelItem>();

    // Resolve the saved viewpoint based on the selection
    // Makes the view active on the main thread.

    var success = false;

    new Invoker().Invoke(
      (Action)(
        () =>
        {
          var savedViewpoint = ResolveSavedViewpoint(selection);
          if (savedViewpoint != null && !savedViewpoint.ContainsVisibilityOverrides)
            return;

          Application.ActiveDocument.SavedViewpoints.CurrentSavedViewpoint = savedViewpoint;
          success = true;
        }
      )
    );

    if (!success)
      return Enumerable.Empty<ModelItem>();

    var models = Application.ActiveDocument.Models;
    Application.ActiveDocument.CurrentSelection.Clear();

    for (var i = 0; i < models.Count; i++)
    {
      var model = models.ElementAt(i);
      var rootItem = model.RootItem;
      if (!rootItem.IsHidden)
        _uniqueModelItems.Add(rootItem);

      ProgressBar.Update(i + 1 / (double)models.Count);
    }

    // End the progress sub-operation
    ProgressBar.EndSubOperation();

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
    var flattenedViewpointList = Application.ActiveDocument.SavedViewpoints.RootItem.Children
      .Select(GetViews)
      .Where(x => x != null)
      .SelectMany(node => node.Flatten())
      .Select(node => new { node.Reference, node.Guid })
      .ToList();

    // Find a match based on the saved view reference
    var viewPointMatch = flattenedViewpointList.FirstOrDefault(
      node => node.Guid.ToString() == savedViewReference || node.Reference == savedViewReference
    );

    if (viewPointMatch != null)
      return ResolveSavedViewpointMatch(savedViewReference);
    {
      foreach (var node in flattenedViewpointList)
      {
        if (node.Guid.ToString() != savedViewReference)
          if (node.Reference != savedViewReference)
            continue;

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
  /// <param name="viewpointMatch">The dynamic object representing the viewpoint match.</param>
  /// <param name="savedViewReference">The saved view reference to resolve.</param>
  /// <returns>The resolved SavedViewpoint.</returns>
  private SavedViewpoint ResolveSavedViewpointMatch(string savedViewReference)
  {
    if (Guid.TryParse(savedViewReference, out var guid))
      // Even though we may have already got a match, that could be to a generic Guid from earlier versions of Navisworks
      if (savedViewReference != Guid.Empty.ToString())
        return (SavedViewpoint)Application.ActiveDocument.SavedViewpoints.ResolveGuid(guid);

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
        // TODO: Determine whether to return null or an empty TreeNode or based on current visibility
        return null;
      case GroupItem groupItem:
        foreach (var childItem in groupItem.Children)
        {
          treeNode.IsEnabled = false;
          treeNode.Elements.Add(GetViews(childItem));
        }
        break;
    }

    // Return the treeNode
    return treeNode;
  }

  /// <summary>
  /// Retrieves the model items from the saved sets.
  /// </summary>
  private IEnumerable<ModelItem> GetObjectsFromSavedSets()
  {
    _uniqueModelItems.Clear();

    // Saved Sets filter stores Guids of the selection sets. This can be converted to ModelItem pseudoIds
    var selections = _filter.Selection.Select(guid => new Guid(guid)).ToList();
    var savedItems = selections.Select(Application.ActiveDocument.SelectionSets.ResolveGuid).OfType<SelectionSet>();

    foreach (var item in savedItems)
      if (item.HasExplicitModelItems)
        _uniqueModelItems.AddRange(item.ExplicitModelItems);
      else if (item.HasSearch)
        _uniqueModelItems.AddRange(item.Search.FindAll(Application.ActiveDocument, false));

    return _uniqueModelItems;
  }

  /// <summary>
  /// Populates the hierarchy by adding ancestor and descendant items to the unique model items.
  /// The unique model items have already been processed to validate that they are not hidden.
  /// </summary>
  public void PopulateHierarchyAndOmitHidden()
  {
    if (_uniqueModelItems == null || !_uniqueModelItems.Any())
      return;

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
            return Enumerable.Empty<ModelItem>();

          var trimmedAncestors = targetFirstObjectChild.Ancestors
            .TakeWhile(ancestor => ancestor != firstObjectAncestor)
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
        allAncestors.Count,
        "Brb, time traveling to find your data's great-grandparents...",
        i =>
        {
          _uniqueModelItems.Add(allAncestors.ElementAt(i));
          return true;
        },
        0.05
      );
    }

    _visited = new HashSet<ModelItem>();
    _descendantProgress = 0;
    var allDescendants = startNodes.SelectMany(e => e.Descendants).Distinct().Count();

    ProgressBar.BeginSubOperation(0.1, "Validating descendants...");

    foreach (var node in startNodes)
      TraverseDescendants(node, allDescendants);
    ProgressBar.EndSubOperation();
  }

  /// <summary>
  /// Traverses the descendants of a given model item and updates a progress bar.
  /// </summary>
  /// <param name="startNode">The starting node for traversal.</param>
  /// <param name="totalDescendants">The total number of descendants.</param>
  private void TraverseDescendants(ModelItem startNode, int totalDescendants)
  {
    var descendantInterval = Math.Max(totalDescendants / 100.0, 1);
    var validDescendants = new HashSet<ModelItem>();
    int lastPercentile = 0;

    Stack<ModelItem> stack = new();
    stack.Push(startNode);

    while (stack.Count > 0)
    {
      if (ProgressBar.IsCanceled)
        _progressViewModel.CancellationTokenSource.Cancel();
      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      ModelItem currentNode = stack.Pop();

      if (_visited.Contains(currentNode))
        continue;
      _visited.Add(currentNode);

      if (currentNode.IsHidden)
      {
        var descendantsCount = currentNode.Descendants.Count();
        _descendantProgress += descendantsCount + 1;
      }
      else
      {
        validDescendants.Add(currentNode);
        _descendantProgress++;
      }

      if (currentNode.Children.Any())
        foreach (var child in currentNode.Children.Where(e => !e.IsHidden))
          stack.Push(child);

      _uniqueModelItems.AddRange(validDescendants);

      int currentPercentile = (int)(_descendantProgress / descendantInterval);
      if (currentPercentile <= lastPercentile)
        continue;
      double progress = _descendantProgress / (double)totalDescendants;
      ProgressBar.Update(progress);
      lastPercentile = currentPercentile;
    }
  }

  /// <summary>
  /// Executes a given function while updating a progress bar.
  /// </summary>
  /// <param name="totalCount">The total number of iterations.</param>
  /// <param name="operationName">The name of the operation.</param>
  /// <param name="fn">The function to execute on each iteration.</param>
  /// <param name="fractionOfRemainingTime">The fraction of remaining time for the operation (optional).</param>
  private void ProgressLooper(
    int totalCount,
    string operationName,
    Func<int, bool> fn,
    double fractionOfRemainingTime = 0
  )
  {
    var increment = 1.0 / totalCount != 0 ? 1.0 / totalCount : 1.0;
    var updateInterval = Math.Max(totalCount / 100, 1);
    ProgressBar.BeginSubOperation(fractionOfRemainingTime, operationName);
    ProgressBar.Update(0);

    for (int i = 0; i < totalCount; i++)
    {
      if (ProgressBar.IsCanceled)
        _progressViewModel.CancellationTokenSource.Cancel();
      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      bool shouldContinue = fn(i);

      if (!shouldContinue)
        break;

      if (i % updateInterval != 0 && i != totalCount)
        continue;

      double progress = (i + 1) * increment;
      ProgressBar.Update(progress);
    }

    ProgressBar.EndSubOperation();
  }

  /// <summary>
  /// Omits items that are hidden from the starting list of nodes if they are not visible in the model.
  /// </summary>
  public void ValidateStartNodes()
  {
    // Remove any nodes that are descendants of hidden nodes.
    _uniqueModelItems.RemoveWhere(e => e.AncestorsAndSelf.Any(a => a.IsHidden));
  }
}
