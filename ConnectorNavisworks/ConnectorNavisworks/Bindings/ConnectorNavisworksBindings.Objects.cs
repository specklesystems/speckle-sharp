using DesktopUI2.Models.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Cursor = System.Windows.Forms.Cursor;
using System.Windows.Forms;

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
      List<string> filteredObjects = new List<string>();

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

        default:
          return filteredObjects;
      }
    }

    private static IEnumerable<string> GetObjectsFromSelection(ISelectionFilter filter)
    {
      // Manual Selection is a straightforward collection of the pseudoIds

      // TODO: Handle a resorted selection Tree. Effectively this could be:
      // a) Delete any saved streams based on Manual Selection
      // b) Change what is stored to allow for a cross check that the pseudoId still matches the original item at the path
      // c) As a SelectionTree isChanging event load in all manual selection saved states and watch the changes and rewrite the result

      return filter.Selection;
    }

    private static IEnumerable<string> GetObjectsFromSavedSets(ISelectionFilter filter)
    {
      Cursor.Current = Cursors.WaitCursor;
      // Saved Sets filter stores Guids of the selection sets. This can be converted to ModelItem pseudoIds
      List<Guid> selections = filter.Selection.Select(guid => new Guid(guid)).ToList();
      List<SelectionSet> savedItems = selections
        .Select(Doc.SelectionSets.ResolveGuid)
        .Cast<SelectionSet>()
        .ToList();

      HashSet<string> objectPseudoIds = new HashSet<string>();

      savedItems.ForEach(
        item =>
        {
          // If the Saved Set is a Selection, add all the saved items and map to pseudoIds
          if (item.HasExplicitModelItems)
            objectPseudoIds.UnionWith(item.ExplicitModelItems.Select(Utils.GetPseudoId)
            );

          // If the Saved Set is a Search, add all the matching items and map to pseudoIds
          if (item.HasSearch)
            objectPseudoIds
              .UnionWith(item.Search
                .FindAll(Doc, false).Select(Utils.GetPseudoId)
              );
        });

      Cursor.Current = Cursors.Default;
      return objectPseudoIds.ToList();
    }

    private static IEnumerable<string> GetObjectsFromClashResults(ISelectionFilter filter)
    {
      // Clash Results filter stores Guids of the Clash Result groups per Test. This can be converted to ModelItem pseudoIds
      throw new NotImplementedException();
    }
  }
}