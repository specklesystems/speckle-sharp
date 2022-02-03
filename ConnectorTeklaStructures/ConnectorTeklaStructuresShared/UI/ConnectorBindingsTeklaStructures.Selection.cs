using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using Speckle.ConnectorTeklaStructures.Util;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    public override List<string> GetSelectedObjects()
    {
      var names = new List<string>();
      ModelObjectEnumerator myEnum = new Tekla.Structures.Model.UI.ModelObjectSelector().GetSelectedObjects();
      while(myEnum.MoveNext()){
        names.Add(myEnum.Current.Identifier.GUID.ToString());
      }
      return names;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var categories = new List<string>();
      var parameters = new List<string>();
      var views = new List<string>();
      if (Model != null)
      {
        //selectionCount = Model.Selection.GetElementIds().Count();
        //categories = ConnectorTeklaStructuresUtils.GetCategoryNames(Model);
        //parameters = ConnectorTeklaStructuresUtils.GetParameterNames(Model);
        //views = ConnectorTeklaStructuresUtils.GetViewNames(Model);
      }
      return new List<ISelectionFilter>()
            {
            new ManualSelectionFilter(),
            //new ListSelectionFilter {Slug="type", Name = "Categories",
            //    Icon = "Category", Values = objectTypes,
            //    Description="Adds all objects belonging to the selected types"},
        //new PropertySelectionFilter{
        //  Slug="param",
        //  Name = "Param",
        //  Description="Adds  all objects satisfying the selected parameter",
        //  Icon = "FilterList",
        //  HasCustomProperty = false,
        //  Values = objectNames,
        //  Operators = new List<string> {"equals", "contains", "is greater than", "is less than"}
        //},
            new AllSelectionFilter {Slug="all",  Name = "All",
                Icon = "CubeScan", Description = "Selects all document objects." },

            };
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
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
          {
            selection.Add(myEnum.Current);
          }
          return selection;
        //  return GetSelectedObjects();
        case "all":
          myEnum = Model.GetModelObjectSelector().GetAllObjects();
          while (myEnum.MoveNext())
          {
            selection.Add(myEnum.Current);
          }
          return selection;


        //case "type":
        //  var typeFilter = filter as ListSelectionFilter;
        //  if (ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames == null)
        //  {
        //    ConnectorTeklaStructuresUtils.GetObjectIDsTypesAndNames(Model);
        //  }
        //  foreach (var type in typeFilter.Selection)
        //  {
        //    selection.AddRange(ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames
        //        .Where(pair => pair.Value.Item1 == type)
        //        .Select(pair => pair.Key)
        //        .ToList());
        //  }
        //  return selection;
      }

      return selection;
    }
  }
}
