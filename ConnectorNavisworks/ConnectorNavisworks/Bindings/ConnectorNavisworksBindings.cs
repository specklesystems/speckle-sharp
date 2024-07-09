using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2;
using DesktopUI2.Models;
using Speckle.ConnectorNavisworks.NavisworksOptions;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using static Speckle.ConnectorNavisworks.Other.SpeckleNavisworksUtilities;
using MenuItem = DesktopUI2.Models.MenuItem;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks : ConnectorBindings
{
  // Much of the interaction in Navisworks is through the ActiveDocument API
  private static Document s_activeDoc;
  internal static Control Control;
  private static object s_cachedCommit;

  internal static List<Base> CachedConvertedElements;
  private static StreamState s_cachedState;
  private ISpeckleKit _defaultKit;
  private ISpeckleConverter _navisworksConverter;

  // private bool _isRetrying;
  internal static bool PersistCache;

  private readonly NavisworksOptionsManager _settingsHandler;

  /// <summary>
  /// Gets a new instance of a commit object with initial properties.
  /// </summary>
  private static Collection CommitObject =>
    new()
    {
      ["units"] = GetUnits(s_activeDoc),
      collectionType = "Navisworks Model",
      name = s_activeDoc.Title
    };

  public ConnectorBindingsNavisworks(Document navisworksActiveDocument)
  {
    s_activeDoc = navisworksActiveDocument;
    s_activeDoc.SelectionSets.ToSavedItemCollection();

    // Sets the Main Thread Control to Invoke commands on.
    Control = new Control();
    Control.CreateControl();

    _defaultKit = KitManager.GetDefaultKit();
    _navisworksConverter = _defaultKit?.LoadConverter(VersionedAppName);
    _settingsHandler = new NavisworksOptionsManager();
  }

  public static string HostAppName => HostApplications.Navisworks.Slug;

  public static string HostAppNameVersion => VersionedAppName.Replace("Navisworks", "Navisworks ");

  public static bool CachedConversion =>
    CachedConvertedElements != null && CachedConvertedElements.Count != 0 && s_cachedCommit != null;

  public override string GetActiveViewName() => "Entire Document";

  public override List<MenuItem> GetCustomStreamMenuItems() => new();

  public override string GetHostAppName() => HostAppName;

  public override string GetHostAppNameVersion() => HostAppNameVersion;

  private static string GetDocPath() => "";

  public override string GetDocumentLocation() => GetDocPath();

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
    var fileName = s_activeDoc.CurrentFileName;
    var hash = Core.Models.Utilities.HashString(fileName, Core.Models.Utilities.HashingFunctions.MD5);
    return hash;
  }

  public override List<string> GetObjectsInView() // this returns all visible doc objects.
    // TODO!
    =>
    throw new NotImplementedException();
}
