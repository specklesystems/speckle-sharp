using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2;
using Speckle.Core.Kits;
using Application = Autodesk.Navisworks.Api.Application;
using MenuItem = DesktopUI2.Models.MenuItem;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks : ConnectorBindings
{
  // Much of the interaction in Navisworks is through the ActiveDocument API
  private static Document _doc;
  private static Control _control;
  private ISpeckleKit _defaultKit;
  private ISpeckleConverter _navisworksConverter;

  public ConnectorBindingsNavisworks(Document navisworksActiveDocument)
  {
    _doc = navisworksActiveDocument;
    _doc.SelectionSets.ToSavedItemCollection();

    // Sets the Main Thread Control to Invoke commands on.
    _control = new Control();
    _control.CreateControl();

    _defaultKit = KitManager.GetDefaultKit();
    _navisworksConverter = _defaultKit?.LoadConverter(Utilities.VersionedAppName);
  }

  // Majority of interaction with Speckle will be through the saved selection and search Sets


  public static string HostAppName => HostApplications.Navisworks.Slug;

  public static string HostAppNameVersion => Utilities.VersionedAppName.Replace("Navisworks", "Navisworks ");

  public override string GetActiveViewName()
  {
    return "Entire Document";
  }

  public override List<MenuItem> GetCustomStreamMenuItems()
  {
    return new List<MenuItem>();
  }

  public override string GetHostAppName()
  {
    return HostAppName;
  }

  public override string GetHostAppNameVersion()
  {
    return HostAppNameVersion;
  }

  public override string GetFileName()
  {
    return Application.ActiveDocument != null ? Application.ActiveDocument.CurrentFileName : string.Empty;
  }

  private static string GetDocPath()
  {
    return "";
  }

  public override string GetDocumentLocation()
  {
    return GetDocPath();
  }

  public override void SelectClientObjects(List<string> objs, bool deselect = false)
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
    var fileName = _doc.CurrentFileName;
    var hash = Core.Models.Utilities.hashString(fileName, Core.Models.Utilities.HashingFuctions.MD5);
    return hash;
  }
}
