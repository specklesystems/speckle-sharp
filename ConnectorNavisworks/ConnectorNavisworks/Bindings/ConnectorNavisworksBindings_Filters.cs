using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            List<ObjectHierarchy> savedSelectionSets = selectSetsRootItem.Children.Select(GetSets).OfType<ObjectHierarchy>().ToList();

            if (savedSelectionSets.Count > 0)
            {
                var selectionSetsFilter = new TreeSelectionFilter
                {
                    Slug = "sets", Name = "Saved Sets and Selections", Icon = "FileTree",
                    Description = "Select saved selection and search sets to include in the commit.",
                    Values = savedSelectionSets
                };
                filters.Add(selectionSetsFilter);
            }

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

        private static ObjectHierarchy GetSets(SavedItem savedItem)
        {

            var hierarchyObject = new ObjectHierarchy
            {
                DisplayName = savedItem.DisplayName,
                Guid = savedItem.Guid,
                IndexWith = nameof(ObjectHierarchy.Guid),
                Indices = Doc.SelectionSets.CreateIndexPath(savedItem).ToArray()
            };

            if (!savedItem.IsGroup) return hierarchyObject;

            //iterate the children and output
            foreach (SavedItem childItem in ((GroupItem)savedItem).Children)
            {
                hierarchyObject.Elements.Add(GetSets(childItem));
            }

            return hierarchyObject.Elements.Count > 0 ? hierarchyObject : null;
        }

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