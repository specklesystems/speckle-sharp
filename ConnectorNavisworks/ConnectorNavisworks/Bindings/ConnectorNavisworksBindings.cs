using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorNavisworks.NavisworksOptions;
using Speckle.ConnectorNavisworks.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Application = Autodesk.Navisworks.Api.Application;
using Cursor = System.Windows.Forms.Cursor;
using MenuItem = DesktopUI2.Models.MenuItem;
using Utilities = Speckle.ConnectorNavisworks.Other.Utilities;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks : ConnectorBindings
{
  // Much of the interaction in Navisworks is through the ActiveDocument API
  private static Document _doc;
  internal static Control Control;
  private static object _cachedCommit;

  internal static List<Base> CachedConvertedElements;
  private static StreamState _cachedState;
  private ISpeckleKit _defaultKit;
  private ISpeckleConverter _navisworksConverter;

  // private bool _isRetrying;
  internal static bool PersistCache;

  private NavisworksOptionsManager _settingsHandler;

  public ConnectorBindingsNavisworks(Document navisworksActiveDocument)
  {
    _doc = navisworksActiveDocument;
    _doc.SelectionSets.ToSavedItemCollection();

    // Sets the Main Thread Control to Invoke commands on.
    Control = new Control();
    Control.CreateControl();

    _defaultKit = KitManager.GetDefaultKit();
    _navisworksConverter = _defaultKit?.LoadConverter(Utilities.VersionedAppName);
    _settingsHandler = new NavisworksOptionsManager();
  }

  public static string HostAppName => HostApplications.Navisworks.Slug;

  public static string HostAppNameVersion => Utilities.VersionedAppName.Replace("Navisworks", "Navisworks ");

  public static bool CachedConversion =>
    CachedConvertedElements != null && CachedConvertedElements.Any() && _cachedCommit != null;

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
    var hash = Core.Models.Utilities.HashString(fileName, Core.Models.Utilities.HashingFunctions.MD5);
    return hash;
  }

  public override List<string> GetObjectsInView() // this returns all visible doc objects.
  // TODO!
  {
    throw new NotImplementedException();
  }

  public async Task RetryLastConversionSend()
  {
    if (_doc == null)
      return;

    if (CachedConvertedElements == null || _cachedCommit == null)
      throw new SpeckleException("Cant retry last conversion: no cached conversion or commit found.");

    if (_cachedCommit is Collection commitObject)
    {
      // _isRetrying = true;

      var applicationProgress = Application.BeginProgress("Retrying that send to Speckle.");
      _progressBar = new ProgressInvoker(applicationProgress);
      _progressViewModel = new ProgressViewModel();

      commitObject.elements = CachedConvertedElements;

      var state = _cachedState;
      
      _progressBar.BeginSubOperation(0.7, "Retrying cached conversion.");
      _progressBar.EndSubOperation();
      
      var objectId = await SendConvertedObjectsToSpeckle(state, commitObject).ConfigureAwait(false);

      if (_progressViewModel.Report.OperationErrors.Any())
        ConnectorHelpers.DefaultSendErrorHandler("", _progressViewModel.Report.OperationErrors.Last());

      _progressViewModel.CancellationToken.ThrowIfCancellationRequested();

      state.Settings.Add(new CheckBoxSetting { Slug = "retrying", IsChecked = true });

      string commitId;
      try
      {
        commitId = await CreateCommit(state, objectId).ConfigureAwait(false);
      }
      finally
      {
        _progressBar.EndSubOperation();
        Application.EndProgress();
        Cursor.Current = Cursors.Default;
      }

      state.Settings.RemoveAll(x => x.Slug == "retrying");

      if (string.IsNullOrEmpty(commitId))
        return;
    }

    // nullify the cached conversion and commit on success.
    _cachedCommit = null;

    CachedConvertedElements = null;
    // _isRetrying = false;
  }
}
