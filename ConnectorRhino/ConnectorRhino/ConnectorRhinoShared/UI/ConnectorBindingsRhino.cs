using System.Collections.Generic;
using System.Linq;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Utilities = Speckle.Core.Models.Utilities;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
  private static string SpeckleKey = "speckle2";
  private static string UserStrings = "userStrings";
  private static string UserDictionary = "userDictionary";
  private static string ApplicationIdKey = "applicationId";
  public Dictionary<string, Base> StoredObjectParams = new(); // these are to store any parameters found on parent objects to add to fallback objects

  public Dictionary<string, Base> StoredObjects = new();

  public ConnectorBindingsRhino()
  {
    RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
    RhinoDoc.LayerTableEvent += RhinoDoc_LayerChange; // used to update the DUI2 layer filter to reflect layer changes
  }

  public RhinoDoc Doc => RhinoDoc.ActiveDoc;
  public List<ApplicationObject> Preview { get; set; } = new();
  private string SelectedReceiveCommit { get; set; }

  public override List<ReceiveMode> GetReceiveModes()
  {
    return new List<ReceiveMode> { ReceiveMode.Update, ReceiveMode.Create };
  }

  // used to store the Stream State settings when sending/receiving
  private List<ISetting>? CurrentSettings { get; set; }

  #region Local streams I/O with local file

  public override List<StreamState> GetStreamsInFile()
  {
    var strings = Doc?.Strings.GetEntryNames(SpeckleKey);

    if (strings == null)
      return new List<StreamState>();

    var states = strings
      .Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue(SpeckleKey, s)))
      .ToList();
    return states;
  }

  public override void WriteStreamsToFile(List<StreamState> streams)
  {
    Doc.Strings.Delete(SpeckleKey);
    foreach (var s in streams)
      Doc.Strings.SetString(SpeckleKey, s.StreamId, JsonConvert.SerializeObject(s));
  }

  #endregion

  #region boilerplate

  public override string GetActiveViewName()
  {
    return "Entire Document"; // Note: rhino does not have views that filter objects.
  }

  public override List<string> GetObjectsInView()
  {
    var objs = Doc.Objects.Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList(); // Note: this returns all the doc objects.
    return objs;
  }

  public override string GetHostAppNameVersion()
  {
    return Utils.RhinoAppName;
  }

  public override string GetHostAppName()
  {
    return HostApplications.Rhino.Slug;
  }

  public override string GetDocumentId()
  {
    return Utilities.HashString("X" + Doc?.Path + Doc?.Name, Utilities.HashingFunctions.MD5);
  }

  public override string GetDocumentLocation()
  {
    return Doc?.Path;
  }

  public override string GetFileName()
  {
    return Doc?.Name;
  }

  //improve this to add to log??
  private void LogUnsupportedObjects(List<RhinoObject> objs, ISpeckleConverter converter)
  {
    var reportLog = new Dictionary<string, int>();
    foreach (var obj in objs)
    {
      var type = obj.ObjectType.ToString();
      if (reportLog.ContainsKey(type))
        reportLog[type] = reportLog[type]++;
      else
        reportLog.Add(type, 1);
    }
    RhinoApp.WriteLine("Deselected unsupported objects:");
    foreach (var entry in reportLog)
      RhinoApp.WriteLine($"{entry.Value} of type {entry.Key}");
  }

  public override void ResetDocument()
  {
    if (PreviewConduit != null)
      PreviewConduit.Enabled = false;
    else
      Doc.Objects.UnselectAll(false);

    Doc.Views.Redraw();
  }

  #endregion

  public override List<MenuItem> GetCustomStreamMenuItems()
  {
    return new List<MenuItem>();
  }
}
