using System.Collections.Generic;
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
    public partial class ConnectorBindingsNavisworks : ConnectorBindings
    {


        public override List<ISelectionFilter> GetSelectionFilters()
        {
            return new List<ISelectionFilter>()
            {
                new ManualSelectionFilter(),
                new AllSelectionFilter
                {
                    Slug = "all", Name = "Everything", Icon = "CubeScan", Description = "Selects all document objects."
                }

            };
        }


        public override List<string> GetSelectedObjects()
        {
            var objects = new List<string>();
            return objects;
        }
    }
}