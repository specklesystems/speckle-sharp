using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
using DesktopUI2.Models.Filters;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var filters = new List<ISelectionFilter>();

      var manualFilter = new ManualSelectionFilter();

      if (Doc == null) return filters;

      filters.Add(manualFilter);

      var selectSetsRootItem = Doc.SelectionSets.RootItem;

      var savedSelectionSets = selectSetsRootItem.Children.Select(GetSets)?.OfType<TreeNode>().ToList();

      if (savedSelectionSets.Count > 0)
      {
        var selectionSetsFilter = new TreeSelectionFilter
        {
          Slug = "sets", Name = "Saved Sets and Selections", Icon = "FileTree",
          Description = "Select saved selection and search sets to include in the commit.",
          Values = savedSelectionSets
        };
        filters.Add(selectionSetsFilter);
      }

      //var clashPlugin = Doc.GetClash();
      //var clashTests = clashPlugin.TestsData;
      //var groupedClashResults = clashTests.Tests.Select(GetClashTestResults).OfType<TreeNode>().ToList();

      //if (groupedClashResults.Count >= 0)
      //{
      //  var clashReportFilter = new TreeSelectionFilter
      //  {
      //    Slug = "clashes", Name = "Clash Detective Results", Icon = "MessageAlert",
      //    Description = "Select group clash test results.",
      //    Values = groupedClashResults
      //  };
      //  filters.Add(clashReportFilter);
      //}


      return filters;
    }

    private static TreeNode GetSets(SavedItem savedItem)
    {
      var treeNode = new TreeNode
      {
        DisplayName = savedItem.DisplayName,
        Guid = savedItem.Guid,
        IndexWith = nameof(TreeNode.Guid),
        Indices = Doc.SelectionSets.CreateIndexPath(savedItem).ToArray()
      };

      if (!savedItem.IsGroup) return treeNode;

      //iterate the children and output
      foreach (var childItem in ((GroupItem)savedItem).Children) treeNode.Elements.Add(GetSets(childItem));

      return treeNode.Elements.Count > 0 ? treeNode : null;
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
        if (result.IsGroup)
          treeNode.Elements.Add(new TreeNode
          {
            DisplayName = result.DisplayName,
            Guid = result.Guid,
            IndexWith = nameof(TreeNode.Guid)
          });

      return treeNode.Elements.Count > 0 ? treeNode : null;
    }
  }
}