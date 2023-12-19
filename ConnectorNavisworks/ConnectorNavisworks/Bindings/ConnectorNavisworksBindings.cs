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
using static Speckle.ConnectorNavisworks.Other.SpeckleNavisworksUtilities;
using Application = Autodesk.Navisworks.Api.Application;
using Cursor = System.Windows.Forms.Cursor;
using MenuItem = DesktopUI2.Models.MenuItem;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks : ConnectorBindings
{
  // Much of the interaction in Navisworks is through the ActiveDocument API
  private static Document s_doc;
  internal static Control Control;
  private static object s_cachedCommit;

  internal static List<Base> CachedConvertedElements;
  private static StreamState s_cachedState;
  private ISpeckleKit _defaultKit;
  private ISpeckleConverter _navisworksConverter;

  // private bool _isRetrying;
  internal static bool PersistCache;

  private readonly NavisworksOptionsManager _settingsHandler;

  public ConnectorBindingsNavisworks(Document navisworksActiveDocument)
  {
    s_doc = navisworksActiveDocument;
    s_doc.SelectionSets.ToSavedItemCollection();

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
    var fileName = s_doc.CurrentFileName;
    var hash = Core.Models.Utilities.HashString(fileName, Core.Models.Utilities.HashingFunctions.MD5);
    return hash;
  }

  public override List<string> GetObjectsInView() // this returns all visible doc objects.
    // TODO!
    =>
    throw new NotImplementedException();

  public async Task RetryLastConversionSend()
  {
    if (s_doc == null)
    {
      return;
    }

    if (CachedConvertedElements == null || s_cachedCommit == null)
    {
      throw new SpeckleException("Cant retry last conversion: no cached conversion or commit found.");
    }

    if (s_cachedCommit is Collection commitObject)
    {
      // _isRetrying = true;

      var applicationProgress = Application.BeginProgress("Retrying that send to Speckle.");
      _progressBar = new ProgressInvoker(applicationProgress);
      _progressViewModel = new ProgressViewModel();

      commitObject.elements = CachedConvertedElements;

      var state = s_cachedState;

      _progressBar.BeginSubOperation(0.7, "Retrying cached conversion.");
      _progressBar.EndSubOperation();

      var objectId = await SendConvertedObjectsToSpeckle(state, commitObject).ConfigureAwait(false);

      if (_progressViewModel.Report.OperationErrors.Count != 0)
      {
        ConnectorHelpers.DefaultSendErrorHandler("", _progressViewModel.Report.OperationErrors.Last());
      }

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
      {
        return;
      }
    }

    // nullify the cached conversion and commit on success.
    s_cachedCommit = null;

    CachedConvertedElements = null;
    // _isRetrying = false;
  }
}
