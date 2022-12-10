using DesktopUI2.Models.Filters;
using Speckle.ConnectorNavisworks.Objects;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;

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
      // Saved Sets filter stores Guids of the selection sets. This can be converted to ModelItem pseudoIds
      throw new NotImplementedException();
    }

    private static IEnumerable<string> GetObjectsFromClashResults(ISelectionFilter filter)
    {
      // Clash Results filter stores Guids of the Clash Result groups per Test. This can be converted to ModelItem pseudoIds
      throw new NotImplementedException();
    }
  }
}