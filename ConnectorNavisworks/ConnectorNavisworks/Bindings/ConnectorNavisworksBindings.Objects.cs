using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2.Models.Filters;
using static Speckle.ConnectorNavisworks.Utils;
using Cursor = System.Windows.Forms.Cursor;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    public override List<string> GetObjectsInView() // this returns all visible doc objects.
    {
      var objects = new List<string>();
      return objects;
    }

    private static IEnumerable<string> GetObjectsFromFilter(ISelectionFilter filter)
    {
      var filteredObjects = new List<string>();

      switch (filter.Slug)
      {
        case "manual":
          filteredObjects.AddRange(GetObjectsFromSelection(filter));
          return filteredObjects;

        case "sets":
          filteredObjects.AddRange(GetObjectsFromSavedSets(filter));
          return filteredObjects;

        case "clashes":
          filteredObjects.AddRange(GetObjectsFromClashResults(filter));
          return filteredObjects;

        case "views":
          filteredObjects.AddRange(GetObjectsFromSavedViewpoint(filter));
          return filteredObjects;

        default:
          return filteredObjects;
      }
    }

    private static IEnumerable<string> GetObjectsFromSavedViewpoint(ISelectionFilter filter)
    {
      var selection = filter.Selection.FirstOrDefault();
      if (string.IsNullOrEmpty(selection))
      {
        return Enumerable.Empty<string>();
      }

      var savedViewpoint = ResolveSavedViewpoint(selection);
      if (savedViewpoint == null || !savedViewpoint.ContainsVisibilityOverrides)
      {
        return Enumerable.Empty<string>();
      }

      var items = savedViewpoint.GetVisibilityOverrides().Hidden;
      items.Invert(Doc);

      return items.DescendantsAndSelf.Select(GetPseudoId);
    }

    private static SavedViewpoint ResolveSavedViewpoint(string savedViewReference)
    {
      var flattenedViewpointList = Doc.SavedViewpoints.RootItem.Children
        .Select(GetViews)
        .Where(x => x != null)
        .SelectMany(node => node.Flatten())
        .Select(node => new { Reference = node?.Reference?.Split(':'), node?.Guid })
        .ToList();

      var viewpointMatch = flattenedViewpointList
        .FirstOrDefault(node =>
          node.Guid.ToString() == savedViewReference ||
          node.Reference?.Length == 2 && node.Reference[1] == savedViewReference);

      return viewpointMatch == null
        ? null
        // Resolve the SavedViewpoint
        : ResolveSavedViewpoint(viewpointMatch, savedViewReference);
    }

    private static SavedViewpoint ResolveSavedViewpoint(dynamic viewpointMatch, string savedViewReference)
    {
      if (Guid.TryParse(savedViewReference, out var guid))
      {
        // Even though we may have already got a match, that could be to a generic Guid from earlier versions of Navisworks
        if (savedViewReference != new Guid().ToString()) return (SavedViewpoint)Doc.SavedViewpoints.ResolveGuid(guid);
      }

      if (!(viewpointMatch?.Reference is string[] reference) || reference.Length != 2)
      {
        return null;
      }

      return (SavedViewpoint)Doc.ResolveReference(new SavedItemReference(reference[0], reference[1]));
    }


    private static IEnumerable<string> GetObjectsFromSelection(ISelectionFilter filter)
    {
      // TODO: Handle a resorted selection Tree as this invalidates any saves filter selections. Effectively this could be:
      // a) Delete any saved streams based on Manual Selection
      // b) Change what is stored to allow for a cross check that the pseudoId still matches the original item at the path
      // c) As a SelectionTree isChanging event load in all manual selection saved states and watch the changes and rewrite the result

      // Manual Selection becomes a straightforward collection of the pseudoIds and all descendant nodes
      // The saved filter should store only the visible selected nodes

      var selection = filter.Selection;

      var uniqueIds = new HashSet<string>();

      for (var i = 0; i < selection.Count; i += 1)
      {
        var modelItem = PointerToModelItem(selection[i]);

        if (modelItem == null) continue;

        if (!IsElementVisible(modelItem)) continue;
        var descendants = modelItem.DescendantsAndSelf.ToList();

        for (var j = 0; j < descendants.Count; j += 1)
        {
          var item = descendants[j];
          if (!IsElementVisible(item)) continue;
          uniqueIds.Add(GetPseudoId(item));
        }
      }

      return uniqueIds.ToList();
    }

    private static IEnumerable<string> GetObjectsFromSavedSets(ISelectionFilter filter)
    {
      Cursor.Current = Cursors.WaitCursor;
      // Saved Sets filter stores Guids of the selection sets. This can be converted to ModelItem pseudoIds
      var selections = filter.Selection.Select(guid => new Guid(guid)).ToList();
      var savedItems = selections
        .Select(Doc.SelectionSets.ResolveGuid)
        .Cast<SelectionSet>()
        .ToList();

      var objectPseudoIds = new HashSet<string>();

      savedItems.ForEach(
        item =>
        {
          // If the Saved Set is a Selection, add all the saved items and map to pseudoIds
          if (item.HasExplicitModelItems)
            objectPseudoIds.UnionWith(item.ExplicitModelItems.Select(GetPseudoId)
            );

          // If the Saved Set is a Search, add all the matching items and map to pseudoIds
          if (item.HasSearch)
            objectPseudoIds
              .UnionWith(item.Search
                .FindAll(Doc, false).Select(GetPseudoId)
              );
        });

      Cursor.Current = Cursors.Default;
      return objectPseudoIds.ToList();
    }

    // ReSharper disable once UnusedParameter.Local
    private static IEnumerable<string> GetObjectsFromClashResults(ISelectionFilter unused)
    {
      // Clash Results filter stores Guid of the Clash Result groups per Test. This can be converted to ModelItem pseudoIds
      throw new NotImplementedException();
    }
  }
}