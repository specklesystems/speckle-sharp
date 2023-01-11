using DesktopUI2;
using DesktopUI2.Models.Filters;
using Speckle.ConnectorCSI.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings
  {
    public override List<string> GetSelectedObjects()
    {
      var names = new List<string>();
      var util = new ConnectorCSIUtils();
      var typeNameTupleList = ConnectorCSIUtils.SelectedObjects(Model);
      if (typeNameTupleList == null) return new List<string>() { };
      foreach (var item in typeNameTupleList)
      {
        (string typeName, string name) = item;
        if (ConnectorCSIUtils.IsTypeCSIAPIUsable(typeName))
          names.Add(string.Concat(typeName, ": ", name));
      }
      if (names.Count == 0)
        return new List<string>() { };

      return names;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var filters = new List<ISelectionFilter>();
      filters.Add(new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Selects all document objects."
      });
      filters.Add(new ManualSelectionFilter());

      if (Model != null)
      {
        ConnectorCSIUtils.GetObjectIDsTypesAndNames(Model);
        var objectTypes = ConnectorCSIUtils.ObjectIDsTypesAndNames
            .Select(pair => pair.Value.Item1).Distinct().ToList();
        
        if (objectTypes.Any())
          filters.Add(new ListSelectionFilter
          {
            Slug = "type",
            Name = "Categories",
            Icon = "Category",
            Values = objectTypes,
            Description = "Adds all objects belonging to the selected types."
          });

        string[] groupNames = new string[0];
        int numNames = 0;
        Model.GroupDef.GetNameList(ref numNames, ref groupNames);
        if (groupNames.Any())
          filters.Add(new ListSelectionFilter
          {
            Slug = "group",
            Name = "Group",
            Icon = "SelectGroup",
            Values = groupNames.ToList(),
            Description = "Add all objects belonging to CSI Group."
          });
      }

      return filters;
    }

    public override void SelectClientObjects(List<string> args, bool deselect = false)
    {
      // TODO!
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
          ConnectorCSIUtils.GetObjectIDsTypesAndNames(Model);

          selection.AddRange(ConnectorCSIUtils.ObjectIDsTypesAndNames
                      .Select(pair => pair.Key).ToList());
          return selection;

        case "type":
          var typeFilter = filter as ListSelectionFilter;
          ConnectorCSIUtils.GetObjectIDsTypesAndNames(Model);

          foreach (var type in typeFilter.Selection)
          {
            selection.AddRange(ConnectorCSIUtils.ObjectIDsTypesAndNames
                .Where(pair => pair.Value.Item1 == type)
                .Select(pair => pair.Key)
                .ToList());
          }
          return selection;

        case "group":
          //Clear objects first
          Model.SelectObj.ClearSelection();
          var groupFilter = filter as ListSelectionFilter;
          foreach (var group in groupFilter.Selection)
          { Model.SelectObj.Group(group); }

          return GetSelectedObjects();
          
      }

      return selection;

    }
  }
}