using ConnectorCSI.Storage;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.ConnectorCSI.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Context;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Kits.ConverterInterfaces;

namespace Speckle.ConnectorCSI.UI;

public partial class ConnectorBindingsCSI : ConnectorBindings
{
  public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
  public Dictionary<string, Base> StoredObjects = new();
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
    ISpeckleConverter converter = kit.LoadConverter(appName);

    // set converter settings as tuples (setting slug, setting selection)
    // for csi, these must go before the SetContextDocument method.
    var settings = new Dictionary<string, string>();
    foreach (var setting in state.Settings)
    {
      settings.Add(setting.Slug, setting.Selection);
    }

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

    //Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

    Preview = FlattenCommitObject(commitObject, converter);
    foreach (var previewObj in Preview)
    {
      progress.Report.Log(previewObj);
    }

    converter.ReceiveMode = state.ReceiveMode;
    // needs to be set for editing to work
    converter.SetPreviousContextObjects(previouslyReceivedObjects);

    progress.CancellationToken.ThrowIfCancellationRequested();

    StreamStateManager.SaveBackupFile(Model);

    using var d0 = LogContext.PushProperty("converterName", converter.Name);
    using var d1 = LogContext.PushProperty("converterAuthor", converter.Author);
    using var d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToNative));
    using var d3 = LogContext.PushProperty("converterSettings", settings);
    using var d4 = LogContext.PushProperty("converterReceiveMode", converter.ReceiveMode);

    var newPlaceholderObjects = ConvertReceivedObjects(converter, progress);

    DeleteObjects(previouslyReceivedObjects, newPlaceholderObjects, progress);

    // The following block of code is a hack to properly refresh the view
    // I've only experienced this bug in ETABS so far
#if ETABS
    if (newPlaceholderObjects.Any(o => o.Status == ApplicationObject.State.Updated))
    {
      RefreshDatabaseTable("Beam Object Connectivity");
    }
#endif

    Model.View.RefreshWindow();
    Model.View.RefreshView();

    state.ReceivedObjects = newPlaceholderObjects;

    return state;
  }

  private List<ApplicationObject> ConvertReceivedObjects(ISpeckleConverter converter, ProgressViewModel progress)
  {
    List<ApplicationObject> conversionResults = new();
    ConcurrentDictionary<string, int> conversionProgressDict = new() { ["Conversion"] = 1 };

    foreach (var obj in Preview)
    {
      if (!StoredObjects.ContainsKey(obj.OriginalId))
      {
        continue;
      }

      progress.CancellationToken.ThrowIfCancellationRequested();

      var @base = StoredObjects[obj.OriginalId];
      using var _0 = LogContext.PushProperty("fromType", @base.GetType());

      try
      {
        var conversionResult = (ApplicationObject)converter.ConvertToNative(@base);

        var finalStatus =
          conversionResult.Status != ApplicationObject.State.Unknown
            ? conversionResult.Status
            : ApplicationObject.State.Created;

        obj.Update(
          status: finalStatus,
          createdIds: conversionResult.CreatedIds,
          converted: conversionResult.Converted,
          log: conversionResult.Log
        );
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        ConnectorHelpers.LogConversionException(ex);

        var failureStatus = ConnectorHelpers.GetAppObjectFailureState(ex);
        obj.Update(status: failureStatus, logItem: ex.Message);
      }

      conversionResults.Add(obj);

      progress.Report.UpdateReportObject(obj);

      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);
    }

    if (converter is IFinalizable finalizable)
    {
      finalizable.FinalizeConversion();
    }

    return conversionResults;
  }

  /// <summary>
  /// Traverses the object graph, returning objects to be converted.
  /// </summary>
  /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
  /// <param name="converter">The converter instance, used to define what objects are convertable</param>
  /// <returns>A flattened list of objects to be converted ToNative</returns>
  private List<ApplicationObject> FlattenCommitObject(Base obj, ISpeckleConverter converter)
  {
    void StoreObject(Base b)
    {
      if (!StoredObjects.ContainsKey(b.id))
      {
        StoredObjects.Add(b.id, b);
      }
    }

    ApplicationObject CreateApplicationObject(Base current)
    {
      ApplicationObject NewAppObj()
      {
        var speckleType = current.speckle_type
          .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
          .LastOrDefault();

        return new ApplicationObject(current.id, speckleType) { applicationId = current.applicationId, };
      }

      //Handle convertable objects
      if (converter.CanConvertToNative(current))
      {
        var appObj = NewAppObj();
        appObj.Convertible = true;
        StoreObject(current);
        return appObj;
      }

      //Handle objects convertable using displayValues
      var fallbackMember = DefaultTraversal.displayValuePropAliases
        .Where(o => current[o] != null)
        .Select(o => current[o])
        .FirstOrDefault();

      if (fallbackMember != null)
      {
        var appObj = NewAppObj();
        var fallbackObjects = GraphTraversal.TraverseMember(fallbackMember).Select(CreateApplicationObject);
        appObj.Fallback.AddRange(fallbackObjects);

        StoreObject(current);
        return appObj;
      }

      return null;
    }

    var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

    var objectsToConvert = traverseFunction
      .Traverse(obj)
      .Select(tc => CreateApplicationObject(tc.current))
      .Where(appObject => appObject != null)
      .Reverse() //just for the sake of matching the previous behaviour as close as possible
      .ToList();

    return objectsToConvert;
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
    Model.DatabaseTables.GetTableForEditingArray(
      floorTableKey,
      "ThisParamIsNotActiveYet",
      ref tableVersion,
      ref fieldsKeysIncluded,
      ref numberRecords,
      ref tableData
    );

    double version = 0;
    string versionString = null;
    Model.GetVersion(ref versionString, ref version);
    var programVersion = versionString;

    // this is a workaround for a CSI bug. The applyEditedTables is looking for "Unique Name", not "UniqueName"
    // this bug is patched in version 20.0.0
    if (programVersion.CompareTo("20.0.0") < 0 && fieldsKeysIncluded[0] == "UniqueName")
    {
      fieldsKeysIncluded[0] = "Unique Name";
    }

    Model.DatabaseTables.SetTableForEditingArray(
      floorTableKey,
      ref tableVersion,
      ref fieldsKeysIncluded,
      numberRecords,
      ref tableData
    );
    Model.DatabaseTables.ApplyEditedTables(
      false,
      ref numFatalErrors,
      ref numErrorMsgs,
      ref numWarnMsgs,
      ref numInfoMsgs,
      ref importLog
    );
  }

  // delete previously sent objects that are no longer in this stream
  private void DeleteObjects(
    IReadOnlyCollection<ApplicationObject> previouslyReceiveObjects,
    IReadOnlyCollection<ApplicationObject> newPlaceholderObjects,
    ProgressViewModel progress
  )
  {
    foreach (var obj in previouslyReceiveObjects)
    {
      if (obj.Converted.Count == 0)
      {
        continue;
      }

      if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
      {
        continue;
      }

      foreach (var o in obj.Converted)
      {
        if (o is not string s)
        {
          continue;
        }

        string[] typeAndName = s.Split(new[] { ConnectorCSIUtils.Delimiter }, StringSplitOptions.None);
        if (typeAndName.Length != 2)
        {
          continue;
        }

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
