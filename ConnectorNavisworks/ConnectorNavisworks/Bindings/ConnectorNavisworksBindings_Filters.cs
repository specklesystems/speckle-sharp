using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Clash;
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
            var clashPlugin = Doc.GetClash();
            var clashTests = clashPlugin.TestsData;
            var groupedClashResults = clashTests.Tests.Select(GetClashTestResults).OfType<ObjectHierarchy>().ToList();

            if (groupedClashResults.Count >= 0)
            {

                var clashReportFilter = new TreeSelectionFilter
                {
                    Slug = "clashes", Name = "Clash Detective Results", Icon = "MessageAlert",
                    Description = "Select group clash test results.",
                    Values = groupedClashResults
                };
                filters.Add(clashReportFilter);
            }

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
        private static ObjectHierarchy GetClashTestResults(SavedItem savedItem)
        {

            var clashTest = (ClashTest)savedItem;

            var hierarchyObject = new ObjectHierarchy
            {
                DisplayName = clashTest.DisplayName,
                Guid = clashTest.Guid,
                IndexWith = nameof(ObjectHierarchy.Guid),
            };

            //iterate the children and output only grouped clashes
            foreach (SavedItem result in clashTest.Children)
            {
                if (result.IsGroup)
                {
                    hierarchyObject.Elements.Add(new ObjectHierarchy
                    {
                        DisplayName = result.DisplayName,
                        Guid = result.Guid,
                        IndexWith = nameof(ObjectHierarchy.Guid)
                    });
                }
            }

            return hierarchyObject.Elements.Count > 0 ? hierarchyObject : null;
        }
    }
}