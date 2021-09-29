using System;
using System.Collections.Generic;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.ConnectorETABS.Util;
using System.Linq;

namespace Speckle.ConnectorETABS.UI
{
    public partial class ConnectorBindingsETABS : ConnectorBindings

    {
        public override List<string> GetSelectedObjects()
        {
            var names = new List<string>();
            var util = new ConnectorETABSUtils();
            var typeNameTupleList = ConnectorETABSUtils.SelectedObjects(Doc);
            if (typeNameTupleList == null) return new List<string>() { };
            foreach (var item in typeNameTupleList)
            {
                (string typeName, string name) = item;
                if (ConnectorETABSUtils.IsTypeETABSAPIUsable(typeName))
                {
                    names.Add(string.Concat(typeName, ": ", name));
                }
            }
            if (names.Count == 0)
            {
                return new List<string>() { };
            }
            return names;
        }

        public override List<ISelectionFilter> GetSelectionFilters()
        {
            var objectTypes = new List<string>();
            //var objectIds = new List<string>();
            if (Doc != null)
            {
                ConnectorETABSUtils.GetObjectIDsTypesAndNames(Doc);
                objectTypes = ConnectorETABSUtils.ObjectIDsTypesAndNames
                    .Select(pair => pair.Value.Item1).Distinct().ToList();
                //objectIds = ConnectorETABSUtils.ObjectIDsTypesAndNames.Select(pair => pair.Key).ToList();

            }

            return new List<ISelectionFilter>()
            {
            new ListSelectionFilter {Slug="type", Name = "Cat",
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
                Icon = "CubeScan", Description = "Selects all document objects." }
            };
        }

        public override void SelectClientObjects(string args)
        {
            throw new NotImplementedException();
        }

        private List<string> GetSelectionFilterObjects(ISelectionFilter filter)
        {
            var doc = Doc.Document;

            var selection = new List<string>();

            switch (filter.Slug)
            {
                case "all":
                    if (ConnectorETABSUtils.ObjectIDsTypesAndNames == null)
                    {
                        ConnectorETABSUtils.GetObjectIDsTypesAndNames(Doc);
                    }
                    selection.AddRange(ConnectorETABSUtils.ObjectIDsTypesAndNames
                                .Select(pair => pair.Key).ToList());
                    return selection;


                case "type":
                    var typeFilter = filter as ListSelectionFilter;
                    if (ConnectorETABSUtils.ObjectIDsTypesAndNames == null)
                    {
                        ConnectorETABSUtils.GetObjectIDsTypesAndNames(Doc);
                    }
                    foreach (var type in typeFilter.Selection)
                    {
                        selection.AddRange(ConnectorETABSUtils.ObjectIDsTypesAndNames
                            .Where(pair => pair.Value.Item1 == type)
                            .Select(pair => pair.Key)
                            .ToList());
                    }
                    return selection;


                    /// ETABS doesn't list fields of different objects. 
                    /// For "param" search, maybe search over the name of
                    /// methods of each type?

                    //case "param":
                    //    try
                    //    {
                    //        if (ConnectorETABSUtils.ObjectTypes.Count == 0)
                    //        {
                    //            var _ = ConnectorETABSUtils.GetObjectTypesAndNames(Doc);
                    //        }

                    //        var propFilter = filter as PropertySelectionFilter;
                    //        var query = new FilteredElementCollector(doc)
                    //          .WhereElementIsNotElementType()
                    //          .WhereElementIsNotElementType()
                    //          .WhereElementIsViewIndependent()
                    //          .Where(x => x.IsPhysicalElement())
                    //          .Where(fi => fi.LookupParameter(propFilter.PropertyName) != null);

                    //        propFilter.PropertyValue = propFilter.PropertyValue.ToLowerInvariant();

                    //        switch (propFilter.PropertyOperator)
                    //        {
                    //            case "equals":
                    //                query = query.Where(fi =>
                    //                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)) == propFilter.PropertyValue);
                    //                break;
                    //            case "contains":
                    //                query = query.Where(fi =>
                    //                  GetStringValue(fi.LookupParameter(propFilter.PropertyName)).Contains(propFilter.PropertyValue));
                    //                break;
                    //            case "is greater than":
                    //                query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
                    //                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                    //                                            fi.LookupParameter(propFilter.PropertyName)) >
                    //                                          double.Parse(propFilter.PropertyValue));
                    //                break;
                    //            case "is less than":
                    //                query = query.Where(fi => RevitVersionHelper.ConvertFromInternalUnits(
                    //                                            fi.LookupParameter(propFilter.PropertyName).AsDouble(),
                    //                                            fi.LookupParameter(propFilter.PropertyName)) <
                    //                                          double.Parse(propFilter.PropertyValue));
                    //                break;
                    //        }

                    //        selection = query.ToList();
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        Log.CaptureException(e);
                    //    }
                    //    return selection;
            }

            return selection;

        }
    }
}