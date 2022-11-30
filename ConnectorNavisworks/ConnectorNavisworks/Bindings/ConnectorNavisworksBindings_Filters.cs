using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.DocumentParts;
using DesktopUI2;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Application = Autodesk.Navisworks.Api.Application;
using Document = Autodesk.Navisworks.Api.Document;
using MenuItem = DesktopUI2.Models.MenuItem;

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

            FolderItem selectSetsRootItem = Doc.SelectionSets.RootItem;

            List<ObjectHierarchy> objectHierarchies = selectSetsRootItem.Children.Select(GetObjectHierarchy).ToList();

            if (objectHierarchies.Count <= 0) return filters;

            var selectionSetsFilter = new TreeSelectionFilter
            {
                Slug = "sets", Name = "Saved Sets and Selections", Icon = "FileTree",
                Description = "Select saved selection and search sets to include in the commit.",
                Values = objectHierarchies
            };
            filters.Add(selectionSetsFilter);

            return filters;
        }

        private static ObjectHierarchy GetObjectHierarchy(SavedItem savedSetItem)
        {
            var hierarchyObject = new ObjectHierarchy
            {
                DisplayName = savedSetItem.DisplayName,
                Guid = savedSetItem.Guid,
                Indices = Doc.SelectionSets.CreateIndexPath(savedSetItem).ToArray(),
                IndexWith = nameof(ObjectHierarchy.Guid)
            };

            if (!savedSetItem.IsGroup) return hierarchyObject;

            //iterate the children and output
            foreach (SavedItem childItem in ((GroupItem)savedSetItem).Children)
            {
                hierarchyObject.Elements.Add(GetObjectHierarchy(childItem));
            }

            return hierarchyObject;
        }
    }
}