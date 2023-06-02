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
  private readonly HashSet<ModelItem> _uniqueModelItems;
  private readonly bool _fullTreeSetting;
  private readonly ProgressViewModel _progressViewModel;
  public ProgressInvoker ProgressBar;

  public SelectionHandler(StreamState state, ProgressViewModel progressViewModel)
  {
    _progressViewModel = progressViewModel;
    _filter = state.Filter;
    _uniqueModelItems = new HashSet<ModelItem>();
    _fullTreeSetting =
      state.Settings.OfType<CheckBoxSetting>().FirstOrDefault(x => x.Slug == "full-tree")?.IsChecked ?? false;
  }

  public int Count => _uniqueModelItems.Count;
  public IEnumerable<ModelItem> ModelItems => _uniqueModelItems.ToList().AsReadOnly();

  internal void GetFromFilter()
  {
    switch (_filter.Slug)
    {
      case "manual":
        _uniqueModelItems.AddRange(GetObjectsFromSelection());
        break;

      case "sets":
        _uniqueModelItems.AddRange(GetObjectsFromSavedSets());
        break;

      case "clashes":
        // TODO: Implement GetObjectsFromClashResults
        break;

      case "views":
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
    var progressIncrement = 1.0 / count;

    // Begin the progress sub-operation for getting objects from selection
    ProgressBar.BeginSubOperation(0, "Getting objects from selection");

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

    // Get the selection from the filter
    var selection = _filter.Selection.FirstOrDefault();
    if (string.IsNullOrEmpty(selection))
    {
      return Enumerable.Empty<ModelItem>();
    }

    // Resolve the saved viewpoint based on the selection
    var savedViewpoint = ResolveSavedViewpoint(selection);
    if (savedViewpoint == null || !savedViewpoint.ContainsVisibilityOverrides)
    {
      return Enumerable.Empty<ModelItem>();
    }

    // Get the hidden items from the saved viewpoint and invert their visibility
    var items = savedViewpoint.GetVisibilityOverrides().Hidden;
    items.Invert(Application.ActiveDocument);

    // Add the visible items to the unique model items
    _uniqueModelItems.AddRange(items);

    return _uniqueModelItems;
  }

  /// <summary>
  /// Resolves the SavedViewpoint based on the provided saved view reference.
  /// </summary>
  /// <param name="savedViewReference">The saved view reference to resolve.</param>
  /// <returns>The resolved SavedViewpoint.</returns>
  private SavedViewpoint ResolveSavedViewpoint(string savedViewReference)
  {
    // Get a flattened list of viewpoints and their references
    var flattenedViewpointList = Application.ActiveDocument.SavedViewpoints.RootItem.Children
      .Select(GetViews)
      .Where(x => x != null)
      .SelectMany(node => node.Flatten())
      .Select(node => new { Reference = node?.Reference?.Split(':'), node?.Guid })
      .ToList();

    // Find a match based on the saved view reference
    var viewPointMatch = flattenedViewpointList.FirstOrDefault(
      node =>
        node.Guid.ToString() == savedViewReference
        || (node.Reference?.Length == 2 && node.Reference[1] == savedViewReference)
    );

    // If no match is found, return null; otherwise, resolve the SavedViewpoint
    return viewPointMatch == null ? null : ResolveSavedViewpoint(viewPointMatch, savedViewReference);
  }

  /// <summary>
  /// Resolves the SavedViewpoint based on the provided viewpoint match and saved view reference.
  /// </summary>
  /// <param name="viewpointMatch">The dynamic object representing the viewpoint match.</param>
  /// <param name="savedViewReference">The saved view reference to resolve.</param>
  /// <returns>The resolved SavedViewpoint.</returns>
  private SavedViewpoint ResolveSavedViewpoint(dynamic viewpointMatch, string savedViewReference)
  {
    if (Guid.TryParse(savedViewReference, out var guid))
    {
      // Even though we may have already got a match, that could be to a generic Guid from earlier versions of Navisworks
      if (savedViewReference != Guid.Empty.ToString())
      {
        return (SavedViewpoint)Application.ActiveDocument.SavedViewpoints.ResolveGuid(guid);
      }
    }

    if (viewpointMatch?.Reference is not string[] { Length: 2 } reference)
    {
      return null;
    }

    using var savedRef = new SavedItemReference(reference[0], reference[1]);
    using var resolvedReference = Application.ActiveDocument.ResolveReference(savedRef);
    return (SavedViewpoint)resolvedReference;
  }

  /// <summary>
  /// Retrieves the TreeNode representing views for a given SavedItem.
  /// </summary>
  /// <param name="savedItem">The SavedItem for which to retrieve the views.</param>
  /// <returns>The TreeNode representing the views for the given SavedItem.</returns>
  private TreeNode GetViews(SavedItem savedItem)
  {
    // Create a reference to the SavedItem
    var reference = Application.ActiveDocument.SavedViewpoints.CreateReference(savedItem);

    // Create a new TreeNode with properties based on the SavedItem
    var treeNode = new TreeNode
    {
      DisplayName = savedItem.DisplayName,
      Guid = savedItem.Guid,
      IndexWith = nameof(TreeNode.Reference),
      // Rather than version check Navisworks host application we feature check
      // to see if Guid is set correctly on viewpoints.
      Reference = savedItem.Guid.ToString() == Guid.Empty.ToString() ? reference.SavedItemId : savedItem.Guid.ToString()
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
    {
      if (item.HasExplicitModelItems)
      {
        _uniqueModelItems.AddRange(item.ExplicitModelItems);
      }
      else if (item.HasSearch)
      {
        _uniqueModelItems.AddRange(item.Search.FindAll(Application.ActiveDocument, false));
      }
    }

    return _uniqueModelItems;
  }

  /// <summary>
  /// Populates the hierarchy by adding ancestor and descendant items to the unique model items.
  /// Then omits hidden items based on their parent's visibility.
  /// </summary>
  public void PopulateHierarchyAndOmitHidden()
  {
    // Check if _uniqueModelItems is null or empty
    if (_uniqueModelItems == null || !_uniqueModelItems.Any())
      return;

    var itemsToPopulate = new HashSet<ModelItem>();
    var itemsToOmit = new HashSet<ModelItem>();
    var totalItems = _uniqueModelItems.Count;

    int updateInterval;

    ProgressBar.BeginSubOperation(
      0,
      _fullTreeSetting
        ? "Adding all hierarchical nodes. Hold on tight!"
        : "Adding all selection descendant nodes. Buckle up!"
    );

    // Populate itemsToPopulate list
    for (var i = 0; i < totalItems; i++)
    {
      var item = _uniqueModelItems.ElementAt(i);

      // If the item is hidden, add it to the itemsToOmit list and continue
      if (item.AncestorsAndSelf.Any(modelItem => modelItem.IsHidden))
      {
        itemsToOmit.Add(item);
        continue;
      }

      // All Ancestors must therefore be visible, so add them to the itemsToPopulate list
      if (_fullTreeSetting)
      {
        var ancestorCount = item.Ancestors.Count();
        var ancestorIncrement = 1 / (double)ancestorCount;
        updateInterval = Math.Max(ancestorCount / 10, 1); // Update the progress bar every 1% of progress

        ProgressBar.BeginSubOperation(0, "Adding ancestors.");
        ProgressBar.Update(0);

        for (var a = 0; a < ancestorCount; a++)
        {
          _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

          var ancestor = item.Ancestors.ElementAt(a);

          itemsToPopulate.Add(ancestor);

          if (a % updateInterval != 0)
            continue;
          double progress = (a + 1) * ancestorIncrement;
          ProgressBar.Update(progress);
        }
      }
      ProgressBar.EndSubOperation();

      // Add descendants and self to itemsToPopulate if they meet the specified criteria
      var descendants = item.DescendantsAndSelf;
      var descendantsCount = descendants.Count();
      var descendantsIncrement = 1 / (double)descendantsCount;
      updateInterval = Math.Max(descendantsCount / 10, 1); // Update the progress bar every 1% of progress

      ProgressBar.BeginSubOperation(0, "Adding descendants.");
      ProgressBar.Update(0);
      for (var d = 0; d < descendantsCount; d++)
      {
        _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

        var descendant = descendants.ElementAt(d);
        itemsToPopulate.Add(descendant);

        if (d % updateInterval != 0)
          continue;
        double progress = (d + 1) * descendantsIncrement;
        ProgressBar.Update(progress);
      }

      ProgressBar.EndSubOperation();
    }

    ProgressLooper(
      itemsToPopulate.Count,
      "Finding nested hidden nodes",
      i =>
      {
        var item = itemsToPopulate.ElementAt(i);
        if (item.AncestorsAndSelf.Any(a => a.IsHidden))
        {
          // Add hidden items to itemsToOmit list
          itemsToOmit.Add(item);
        }
        return true;
      }
    );

    ProgressLooper(
      itemsToOmit.Count,
      "Omitting hidden nodes. Shhh, they won't know!",
      i =>
      {
        var item = itemsToOmit.ElementAt(i);
        // Remove items marked for omission from _uniqueModelItems
        _uniqueModelItems.Remove(item);
        return true;
      }
    );

    // Add remaining items from itemsToPopulate to _uniqueModelItems
    _uniqueModelItems.AddRange(itemsToPopulate.Except(itemsToOmit));
   
  }

  void ProgressLooper(int totalCount, string operationName, Func<int, bool> fn)
  {
    var increment = 1.0 / totalCount;
    var updateInterval = Math.Max(totalCount / 100, 1);
    ProgressBar.BeginSubOperation(0, operationName);
    ProgressBar.Update(0);

    for (int i = 0; i < totalCount; i++)
    {
      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      bool shouldContinue = fn(i);

      if (!shouldContinue)
        break;

      if (i % updateInterval != 0)
        continue;

      double progress = (i + 1) * increment;
      ProgressBar.Update(progress);
    }

    ProgressBar.EndSubOperation();
  }
}
