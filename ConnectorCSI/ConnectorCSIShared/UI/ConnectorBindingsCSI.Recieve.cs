using ConnectorCSI.Storage;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorCSI.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings
  {
    public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
    public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();
    public override bool CanPreviewReceive => false;
    public override Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      return null;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      Exceptions.Clear();

      var kit = KitManager.GetDefaultKit();
      var appName = GetHostAppVersion(Model);
      var converter = kit.LoadConverter(appName);

      // set converter settings as tuples (setting slug, setting selection)
      // for csi, these must go before the SetContextDocument method.
      var settings = new Dictionary<string, string>();
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      settings.Add("operation", "receive");
      converter.SetConverterSettings(settings);

      converter.SetContextDocument(Model);
      Exceptions.Clear();
      var previouslyReceivedObjects = state.ReceivedObjects;
      
      progress.CancellationToken.ThrowIfCancellationRequested();
      
      Exceptions.Clear();


      Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
      state.LastCommit = commit;
      Base commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
      await ConnectorHelpers.TryCommitReceived(state, commit, GetHostAppVersion(Model), progress.CancellationToken);
      
      Preview.Clear();
      StoredObjects.Clear();

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;
      //Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      };

      Preview = FlattenCommitObject(commitObject, converter);
      foreach (var previewObj in Preview)
        progress.Report.Log(previewObj);

      converter.ReceiveMode = state.ReceiveMode;
      // needs to be set for editing to work 
      converter.SetPreviousContextObjects(previouslyReceivedObjects);

      progress.CancellationToken.ThrowIfCancellationRequested();

      StreamStateManager.SaveBackupFile(Model);

      var newPlaceholderObjects = ConvertReceivedObjects(converter, progress);
      
      DeleteObjects(previouslyReceivedObjects, newPlaceholderObjects, progress);

      // The following block of code is a hack to properly refresh the view
      // I've only experienced this bug in ETABS so far
#if ETABS
      if (newPlaceholderObjects.Any(o => o.Status == ApplicationObject.State.Updated))
        RefreshDatabaseTable("Beam Object Connectivity");
#endif

      Model.View.RefreshWindow();
      Model.View.RefreshView();

      state.ReceivedObjects = newPlaceholderObjects;

      return state;
    }

    private List<ApplicationObject> ConvertReceivedObjects(ISpeckleConverter converter, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      foreach (var obj in Preview)
      {
        if (!StoredObjects.ContainsKey(obj.OriginalId))
          continue;

        var @base = StoredObjects[obj.OriginalId];
        progress.CancellationToken.ThrowIfCancellationRequested();

        try
        {
          var convRes = converter.ConvertToNative(@base);

          switch (convRes)
          {
            case ApplicationObject o:
              placeholders.Add(o);
              obj.Update(status: o.Status, createdIds: o.CreatedIds, converted: o.Converted, log: o.Log);
              progress.Report.UpdateReportObject(obj);
              break;
            default:
              break;
          }
        }
        catch (Exception e)
        {
          obj.Update(status: ApplicationObject.State.Failed, logItem: e.Message);
          progress.Report.UpdateReportObject(obj);
        }

        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      }

      return placeholders;
    }

    /// <summary>
    /// Recurses through the commit object and flattens it. 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    private List<ApplicationObject> FlattenCommitObject(object obj, ISpeckleConverter converter)
    {
      var objects = new List<ApplicationObject>();

      if (obj is Base @base)
      {
        var appObj = new ApplicationObject(@base.id, ConnectorCSIUtils.SimplifySpeckleType(@base.speckle_type)) { applicationId = @base.applicationId, Status = ApplicationObject.State.Unknown };

        if (converter.CanConvertToNative(@base))
        {
          if (StoredObjects.ContainsKey(@base.id))
            return objects;

          appObj.Convertible = true;
          objects.Add(appObj);
          StoredObjects.Add(@base.id, @base);
          return objects;
        }
        else
        {
          foreach (var prop in @base.GetMembers().Keys)
            objects.AddRange(FlattenCommitObject(@base[prop], converter));
          return objects;
        }
      }

      if (obj is IList list && list != null)
      {
        foreach (var listObj in list)
          objects.AddRange(FlattenCommitObject(listObj, converter));
        return objects;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
          objects.AddRange(FlattenCommitObject(kvp.Value, converter));
        return objects;
      }

      else
      {
        if (obj != null && !obj.GetType().IsPrimitive && !(obj is string))
        {
          var appObj = new ApplicationObject(obj.GetHashCode().ToString(), obj.GetType().ToString());
          appObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Receiving this object type is not supported in CSI");
          objects.Add(appObj);
        }
      }

      return objects;
    }

    private void RefreshDatabaseTable(string floorTableKey)
    {
      int tableVersion = 0;
      int numberRecords = 0;
      string[] fieldsKeysIncluded = null;
      string[] tableData = null;
      int numFatalErrors = 0;
      int numWarnMsgs = 0;
      int numInfoMsgs = 0;
      int numErrorMsgs = 0;
      string importLog = "";
      Model.DatabaseTables.GetTableForEditingArray(floorTableKey, "ThisParamIsNotActiveYet", ref tableVersion, ref fieldsKeysIncluded, ref numberRecords, ref tableData);

      double version = 0;
      string versionString = null;
      Model.GetVersion(ref versionString, ref version);
      var programVersion = versionString;

      // this is a workaround for a CSI bug. The applyEditedTables is looking for "Unique Name", not "UniqueName"
      // this bug is patched in version 20.0.0
      if (programVersion.CompareTo("20.0.0") < 0 && fieldsKeysIncluded[0] == "UniqueName")
        fieldsKeysIncluded[0] = "Unique Name";

      Model.DatabaseTables.SetTableForEditingArray(floorTableKey, ref tableVersion, ref fieldsKeysIncluded, numberRecords, ref tableData);
      Model.DatabaseTables.ApplyEditedTables(false, ref numFatalErrors, ref numErrorMsgs, ref numWarnMsgs, ref numInfoMsgs, ref importLog);
    }

    // delete previously sent objects that are no longer in this stream
    private void DeleteObjects(List<ApplicationObject> previouslyReceiveObjects, List<ApplicationObject> newPlaceholderObjects, ProgressViewModel progress)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (obj.Converted.Count == 0 || newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        for (int i = 0; i < obj.Converted.Count; i++)
        {
          if (!(obj.Converted[i] is string s && s.Split(new[] { ConnectorCSIUtils.delimiter }, StringSplitOptions.None) is string[] typeAndName && typeAndName.Length == 2))
            continue;

          switch (typeAndName[0])
          {
            case "Frame":
              Model.FrameObj.Delete(typeAndName[1]);
              break;
            case "Area":
              Model.AreaObj.Delete(typeAndName[1]);
              break;
            default:
              continue;
          }

          obj.Update(status: ApplicationObject.State.Removed);
          progress.Report.Log(obj);
        }
      }
    }
  }
}
