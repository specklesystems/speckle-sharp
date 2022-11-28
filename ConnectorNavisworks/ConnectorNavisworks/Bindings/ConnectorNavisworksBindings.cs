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
    public partial class ConnectorBindingsNavisworks : ConnectorBindings
    {
      
        public ConnectorBindingsNavisworks(Document navisworksActiveDocument) : base()
        {
            Doc = navisworksActiveDocument;
        }


        //private static readonly string ApplicationIdKey = "applicationId";
        public static Document Doc;


        public override List<ReceiveMode> GetReceiveModes()
        {
            // No receive modes available
            return new List<ReceiveMode>
            {
                ReceiveMode.Create
            };
        }

        /// <summary>
        /// Stored Base objects from commit flattening on receive: key is the Base id
        /// </summary>
        public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

        public override string GetActiveViewName() => "Entire Document";


        public override List<MenuItem> GetCustomStreamMenuItems()
        {
            return new List<MenuItem>();
        }


        public override string GetHostAppName() => Utils.Slug;


        public override List<string> GetObjectsInView() // this returns all visible doc objects.
        {
            List<string> objects = new List<string>();
            return objects;
        }

        public override string GetHostAppNameVersion() =>
            Utils.VersionedAppName.Replace("Navisworks", "Navisworks "); //hack for ADSK store;

        public override string GetFileName() => (Application.ActiveDocument != null)
            ? Application.ActiveDocument.CurrentFileName
            : string.Empty;

        private List<ISetting>
            CurrentSettings { get; set; } // used to store the Stream State settings when sending/receiving

        public override List<ISetting> GetSettings() => new List<ISetting> { };

        private string GetDocPath(Document doc) => "";

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