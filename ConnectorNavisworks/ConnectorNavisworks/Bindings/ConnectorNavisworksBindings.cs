using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Application = Autodesk.Navisworks.Api.Application;
using MenuItem = DesktopUI2.Models.MenuItem;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks : ConnectorBindings
  {
    // Much of the interaction in Navisworks is through the ActiveDocument API
    public static Document Doc;
    public static Control Control;
    public ISpeckleKit DefaultKit;
    public ISpeckleConverter NavisworksConverter;

    public ConnectorBindingsNavisworks(Document navisworksActiveDocument)
    {
      Doc = navisworksActiveDocument;
      SavedSets = Doc.SelectionSets.ToSavedItemCollection();

      // Sets the Main Thread Control to Invoke commands on.
      Control = new Control();
      Control.CreateControl();

      DefaultKit = KitManager.GetDefaultKit();
      NavisworksConverter = DefaultKit?.LoadConverter(Utils.VersionedAppName);
    }

    // Majority of interaction with Speckle will be through the saved selection and search Sets
    public SavedItemCollection SavedSets { get; set; }


    public override string GetActiveViewName()
    {
      return "Entire Document";
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public static string HostAppName => HostApplications.Navisworks.Slug;

    public static string HostAppNameVersion => Utils.VersionedAppName.Replace("Navisworks", "Navisworks ");
    
    public override string GetHostAppName() => HostAppName;


    public override string GetHostAppNameVersion() => HostAppNameVersion;

    public override string GetFileName()
    {
      return Application.ActiveDocument != null
        ? Application.ActiveDocument.CurrentFileName
        : string.Empty;
    }

    private static string GetDocPath()
    {
      return "";
    }

    public override string GetDocumentLocation()
    {
      return GetDocPath();
    }

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
      var fileName = Doc.CurrentFileName;
      var hash = Utilities.hashString(fileName, Utilities.HashingFuctions.MD5);
      return hash;
    }
  }
}