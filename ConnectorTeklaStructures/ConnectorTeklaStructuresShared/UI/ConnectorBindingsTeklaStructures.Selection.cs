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
        names.Add(myEnum.Current.Identifier.ToString());
      }
      return names;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var objectTypes = new List<string>();
      //var objectIds = new List<string>();
      string[] groupNames = null;
      var groups = new List<string>();
      int numNames = 0;

      return new List<ISelectionFilter>()
            {
            new ManualSelectionFilter(),
            new ListSelectionFilter {Slug="type", Name = "Categories",
                Icon = "Category", Values = objectTypes,
                Description="Adds all objects belonging to the selected types"},
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

    private List<string> GetSelectionFilterObjects(ISelectionFilter filter)
    {
      var doc = Model;

      var selection = new List<string>();

      switch (filter.Slug)
      {
        case "manual":
          return GetSelectedObjects();
        case "all":
          if (ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames == null)
          {
            ConnectorTeklaStructuresUtils.GetObjectIDsTypesAndNames(Model);
          }
          selection.AddRange(ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames
                      .Select(pair => pair.Key).ToList());
          return selection;


        case "type":
          var typeFilter = filter as ListSelectionFilter;
          if (ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames == null)
          {
            ConnectorTeklaStructuresUtils.GetObjectIDsTypesAndNames(Model);
          }
          foreach (var type in typeFilter.Selection)
          {
            selection.AddRange(ConnectorTeklaStructuresUtils.ObjectIDsTypesAndNames
                .Where(pair => pair.Value.Item1 == type)
                .Select(pair => pair.Key)
                .ToList());
          }
          return selection;
      }

      return selection;
    }
  }
}
