using System.Collections.Generic;
using DesktopUI2;
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
      
        public override List<string> GetObjectsInView() // this returns all visible doc objects.
        {
            List<string> objects = new List<string>();
            return objects;
        }
        
    }
}