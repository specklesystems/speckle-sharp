using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using DesktopUI2;
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
        // Much of the interaction in Navisworks is through the ActiveDocument API
        public static Document Doc;

        // Majority of interaction with Speckle will be through the saved selection and search Sets
        public SavedItemCollection SavedSets { get; set; }

        public ConnectorBindingsNavisworks(Document navisworksActiveDocument) : base()
        {
            Doc = navisworksActiveDocument;
            SavedSets = Doc.SelectionSets.ToSavedItemCollection();
        }


        public override string GetActiveViewName() => "Entire Document";


        public override List<MenuItem> GetCustomStreamMenuItems()
        {
            return new List<MenuItem>();
        }


        public override string GetHostAppName() => HostApplications.Navisworks.Slug;


        public override string GetHostAppNameVersion() =>
            Utils.VersionedAppName.Replace("Navisworks", "Navisworks "); //hack for ADSK store;

        public override string GetFileName() => (Application.ActiveDocument != null)
            ? Application.ActiveDocument.CurrentFileName
            : string.Empty;

        private List<ISetting>
            CurrentSettings { get; set; } // used to store the Stream State settings when sending/receiving

        public override List<ISetting> GetSettings() => new List<ISetting> { };

        private static string GetDocPath(Document doc) => "";

        public override string GetDocumentLocation() => GetDocPath(Doc);

        public override void SelectClientObjects(List<string> args, bool deselect = false)
        {
            // TODO!
        }

        public override void ResetDocument()
        {
            // TODO!
        }


        public override string GetDocumentId()
        {
            // TODO!
            // An unsaved document has no path or filename
            string fileName = Doc.CurrentFileName;
            var hash = Core.Models.Utilities.hashString(fileName, Core.Models.Utilities.HashingFuctions.MD5);
            return hash;
        }
    }
}