using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.ViewModels;
using Speckle.ConnectorTeklaStructures.Util;
using System;
using System.Collections.Generic;
using Tekla.Structures.Model;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    public override List<string> GetSelectedObjects()
    {
      var names = new List<string>();
      ModelObjectEnumerator myEnum = new Tekla.Structures.Model.UI.ModelObjectSelector().GetSelectedObjects();
      while (myEnum.MoveNext())
        names.Add(myEnum.Current.Identifier.GUID.ToString());

      return names;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var categories = new List<string>();
      var parameters = new List<string>();
      var views = new List<string>();
      var phases = new List<string>();
      if (Model != null)
      {
        var phaseCollection = Model.GetPhases();
        foreach (Phase p in phaseCollection)
          phases.Add(p.PhaseName);

        //selectionCount = Model.Selection.GetElementIds().Count();
        categories = ConnectorTeklaStructuresUtils.GetCategoryNames(Model);
        //parameters = ConnectorTeklaStructuresUtils.GetParameterNames(Model);
        //views = ConnectorTeklaStructuresUtils.GetViewNames(Model);
      }
      return new List<ISelectionFilter>()
            {
         new AllSelectionFilter {Slug="all",  Name = "Everything",
                Icon = "CubeScan", Description = "Selects all document objects." },

            new ListSelectionFilter {Slug="type", Name = "Categories",
                Icon = "Category", Values = categories,
                Description="Adds all objects belonging to the selected types"},
            new ListSelectionFilter {Slug="phase", Name = "Phases",
              Icon = "SelectGroup", Values = phases,
              Description="Adds all objects belonging to the selected phase"},

            new ManualSelectionFilter(),
            };
    }

    public override void ResetDocument()
    {
      // TODO!
    }

    public override void SelectClientObjects(List<string> args, bool deselect = false)
    {
      // TODO!
    }

    private List<ModelObject> GetSelectionFilterObjects(ISelectionFilter filter)
    {
      var doc = Model;

      var selection = new List<ModelObject>();

      switch (filter.Slug)
      {
        case "manual":
          ModelObjectEnumerator myEnum = new Tekla.Structures.Model.UI.ModelObjectSelector().GetSelectedObjects();
          while (myEnum.MoveNext())
            selection.Add(myEnum.Current);
          return selection;
        //  return GetSelectedObjects();
        case "all":
          myEnum = Model.GetModelObjectSelector().GetAllObjects();
          while (myEnum.MoveNext())
            selection.Add(myEnum.Current);
          return selection;


        case "phase":
          var phaseFilter = filter as ListSelectionFilter;
          myEnum = Model.GetModelObjectSelector().GetAllObjects();
          while (myEnum.MoveNext())
          {
            foreach (var phase in phaseFilter.Selection)
            {
              Phase phaseTemp = new Phase();
              myEnum.Current.GetPhase(out phaseTemp);
              if (phaseTemp.PhaseName == phase)
                selection.Add(myEnum.Current);
            }
          }

          return selection;
        case "type":
          var catFilter = filter as ListSelectionFilter;
          var categories = ConnectorTeklaStructuresUtils.GetCategories(Model);

          foreach (var cat in catFilter.Selection)
          {
            if (categories.ContainsKey(cat))
            {
              myEnum = Model.GetModelObjectSelector().GetAllObjectsWithType(categories[cat]);
              while (myEnum.MoveNext())
                selection.Add(myEnum.Current);
            }
          }
          return selection;
      }

      return selection;
    }
  }
}
