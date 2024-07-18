using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using DesktopUI2.Models.Filters;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  public override List<ISelectionFilter> GetSelectionFilters()
  {
    var filters = new List<ISelectionFilter>();

    var allFilter = new AllSelectionFilter
    {
      Description =
        "Sending Everything isn't advisable for larger models. Why not break your commits into Model Branches?",
      Name = "Everything",
      Slug = "all"
    };

    var manualFilter = new ManualSelectionFilter();

    if (s_activeDoc == null)
    {
      return filters;
    }

    filters.Add(manualFilter);

    var selectSetsRootItem = s_activeDoc.SelectionSets.RootItem;

    var savedSelectionSets = selectSetsRootItem?.Children.Select(GetSets).ToList() ?? new List<TreeNode>();

    if (savedSelectionSets.Count > 0)
    {
      var selectionSetsFilter = new TreeSelectionFilter
      {
        Slug = "sets",
        Name = "Saved Sets and Selections",
        Icon = "FileTree",
        Description = "Select saved selection and search sets to include in the commit.",
        Values = savedSelectionSets
      };
      filters.Add(selectionSetsFilter);
    }

    var savedViewsRootItem = s_activeDoc.SavedViewpoints.RootItem;

    var savedViews =
      savedViewsRootItem?.Children.Select(GetViews).Select(RemoveNullNodes).Where(x => x != null).ToList()
      ?? new List<TreeNode>();

    if (savedViews.Count > 0)
    {
      var savedViewsFilter = new TreeSelectionFilter
      {
        Slug = "views",
        Name = "Saved Viewpoints",
        Icon = "FileTree",
        Description =
          "Select saved viewpoints and send their visible items in the commit. Only views with Hide/Require attribute checked are listed.",
        Values = savedViews,
        SelectionMode = "Multiple"
      };
      filters.Add(savedViewsFilter);
    }

    DocumentClash clashPlugin = s_activeDoc.GetClash();

    var clashTests = clashPlugin?.TestsData;

    if (clashTests != null)
    {
      // var groupedClashResults = clashTests.Tests.Select(GetClashTestResults).Where(x => x != null).ToList();
      //
      // if (groupedClashResults.Count >= 0)
      // {
      //
      //
      //   var clashReportFilter = new TreeSelectionFilter
      //   {
      //     Slug = "clashes", Name = "Clash Detective Results", Icon = "MessageAlert",
      //     Description = "Select group clash test results.",
      //     Values = groupedClashResults
      //   };
      //   filters.Add(clashReportFilter);
      // }
    }

    filters.Add(allFilter);

    return filters;
  }

  private static TreeNode GetSets(SavedItem savedItem)
  {
    var treeNode = new TreeNode
    {
      DisplayName = savedItem.DisplayName,
      Guid = savedItem.Guid,
      IndexWith = nameof(TreeNode.Guid),
      Indices = s_activeDoc.SelectionSets.CreateIndexPath(savedItem).ToArray()
    };

    if (!savedItem.IsGroup)
    {
      return treeNode;
    }

    //iterate the children and output
    foreach (var childItem in ((GroupItem)savedItem).Children)
    {
      treeNode.Elements.Add(GetSets(childItem));
    }

    return treeNode.Elements.Count > 0 ? treeNode : null;
  }

  private static TreeNode GetViews(SavedItem savedItem)
  {
    var reference = s_activeDoc.SavedViewpoints.CreateReference(savedItem);

    var treeNode = new TreeNode
    {
      DisplayName = savedItem.DisplayName,
      Guid = savedItem.Guid,
      IndexWith = nameof(TreeNode.Reference),
      // Rather than version check Navisworks host application we feature check
      // to see if Guid is set correctly on viewpoints.
      Reference = savedItem.Guid.ToString() == new Guid().ToString() ? reference.SavedItemId : savedItem.Guid.ToString()
    };

    switch (savedItem.IsGroup)
    {
      // TODO: This is a defensive measure to prevent sending millions of objects that are hidden in the
      // current view but not hidden explicitly in the saved view. Optionally if this is not the case we could send visible items in the
      // current view with the viewpoint as it is saved.
      case false when savedItem is SavedViewpoint { ContainsVisibilityOverrides: false }:
        return null;
      case false:
        return treeNode;
      default:
        break; // handles savedItem.IsGroup == true, somewhat redundant
    }

    foreach (var childItem in ((GroupItem)savedItem).Children)
    {
      treeNode.IsEnabled = false;
      treeNode.Elements.Add(GetViews(childItem));
    }

    return treeNode.Elements.Count > 0 ? treeNode : null;
  }

  private static TreeNode RemoveNullNodes(TreeNode node)
  {
    if (node == null)
    {
      return null;
    }

    if (node.Elements.Count == 0)
    {
      return node;
    }

    var elements = node.Elements.Select(RemoveNullNodes).Where(childNode => childNode != null).ToList();

    if (elements.Count == 0)
    {
      return null;
    }

    node.Elements = elements;
    return node;
  }

  private static TreeNode GetClashTestResults(SavedItem savedItem)
  {
    var clashTest = (ClashTest)savedItem;

    var treeNode = new TreeNode
    {
      DisplayName = clashTest.DisplayName,
      Guid = clashTest.Guid,
      IndexWith = nameof(TreeNode.Guid)
    };

    //iterate the children and output only grouped clashes
    foreach (var result in clashTest.Children)
    {
      if (result.IsGroup)
      {
        treeNode.Elements.Add(
          new TreeNode
          {
            DisplayName = result.DisplayName,
            Guid = result.Guid,
            IndexWith = nameof(TreeNode.Guid)
          }
        );
      }
    }

    return treeNode.Elements.Count > 0 ? treeNode : null;
  }
}
