using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Avalonia.Threading;
using ConnectorRevit.Revit;
using ConnectorRevit.Storage;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Interfaces;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Revit.Async;
using RevitSharedResources.Interfaces;
using Serilog;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.ConnectorRevit.UI
{

  public partial class ConnectorBindingsRevit
  {
    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    ///
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      await ReceiveStreamTestable(state, progress, Converter.GetType(), CurrentDoc).ConfigureAwait(false);
      return state;
    }

    private static async Task<IConvertedObjectsCache<Base, Element>> ReceiveStreamTestable(IStreamState state, ProgressViewModel progress, Type converterType, UIDocument UIDoc)
    {
      //make sure to instance a new copy so all values are reset correctly
      var converter = (ISpeckleConverter)Activator.CreateInstance(converterType);
      converter.SetContextDocument(UIDoc.Document);

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      Commit myCommit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken).ConfigureAwait(false);
      state.LastCommit = myCommit;
      Base commitObject = await ConnectorHelpers.ReceiveCommit(myCommit, state, progress);
      await ConnectorHelpers.TryCommitReceived(progress.CancellationToken, state, myCommit, ConnectorRevitUtils.RevitAppName);

      //Preview.Clear();
      //StoredObjects.Clear();

      var storedObjects = new Dictionary<string, Base>();
      var preview = FlattenCommitObject(commitObject, converter, storedObjects);
      foreach (var previewObj in preview)
        progress.Report.Log(previewObj);


      converter.ReceiveMode = state.ReceiveMode;
      // needs to be set for editing to work
      var previousObjects = new StreamStateCache(state);
      converter.SetContextDocument(previousObjects);
      // needs to be set for openings in floors and roofs to work
      converter.SetContextObjects(preview);

      try
      {
        await RevitTask.RunAsync(() => UpdateForCustomMapping(progress, myCommit.sourceApplication, state.Settings, preview, storedObjects));
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Warning(ex, "Could not update receive object with user types");
        progress.Report.LogOperationError(new Exception("Could not update receive object with user types. Using default mapping.", ex));
      }

      var (convertedObjects, exception) = await RevitTask.RunAsync<(IConvertedObjectsCache<Base,Element>,Exception)>(app =>
      {
        string transactionName = $"Baking stream {state.StreamId}";
        using var g = new TransactionGroup(UIDoc.Document, transactionName);
        using var t = new Transaction(UIDoc.Document, transactionName);

        g.Start();
        var failOpts = t.GetFailureHandlingOptions();
        failOpts.SetFailuresPreprocessor(new ErrorEater(converter));
        failOpts.SetClearAfterRollback(true);
        t.SetFailureHandlingOptions(failOpts);
        t.Start();

        try
        {
          converter.SetContextDocument(t);

          var convertedObjects = ConvertReceivedObjects(converter, progress, UIDoc, state.Settings, preview, storedObjects);

          if (state.ReceiveMode == ReceiveMode.Update)
            DeleteObjects(previousObjects, convertedObjects, UIDoc.Document);

          previousObjects.AddConvertedElements(convertedObjects);
          t.Commit();
          g.Assimilate();
          return (convertedObjects, null);
        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Error(ex, "Rolling back connector transaction {transactionName} {transactionType}", transactionName, t.GetType());

          string message = $"Fatal Error: {ex.Message}";
          if (ex is OperationCanceledException) message = "Receive cancelled";
          progress.Report.LogOperationError(new Exception($"{message} - Changes have been rolled back", ex));

          t.RollBack();
          g.RollBack();
          return (null, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
        }
      }).ConfigureAwait(false);

      if (exception != null)
      {
        //Don't wrap cancellation token (if it's ours!)
        if (exception is OperationCanceledException && progress.CancellationToken.IsCancellationRequested) throw exception;
        throw new SpeckleException(exception.Message, exception);
      }

      return convertedObjects;
    }

    //delete previously sent object that are no more in this stream
    private static void DeleteObjects(IReceivedObjectIdMap<Base, Element> previousObjects, IConvertedObjectsCache<Base, Element> convertedObjects, Document document)
    {
      var previousAppIds = previousObjects.GetAllConvertedIds().ToList();
      for (var i = previousAppIds.Count - 1; i >=0; i--)
      {
        var appId = previousAppIds[i];
        if (string.IsNullOrEmpty(appId) || convertedObjects.HasConvertedObjectWithId(appId))
          continue;

        var elementIdToDelete = previousObjects.GetCreatedIdsFromConvertedId(appId);

        foreach (var elementId in elementIdToDelete)
        {
          var elementToDelete = document.GetElement(elementId);

          if (elementToDelete != null) document.Delete(elementToDelete.Id);
          previousObjects.RemoveConvertedId(appId);
        }
      }
    }

    private static IConvertedObjectsCache<Base, Element> ConvertReceivedObjects(ISpeckleConverter converter, ProgressViewModel progress, UIDocument UIDoc, List<ISetting> settings, List<ApplicationObject> preview, Dictionary<string, Base> storedObjects)
    {
      var convertedObjectsCache = new ConvertedObjectsCache();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = settings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting;
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;
      foreach (var obj in preview)
      {
        var @base = storedObjects[obj.OriginalId];
        progress.CancellationToken.ThrowIfCancellationRequested();

        try
        {
          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          var s = new CancellationTokenSource();
          DispatcherTimer.RunOnce(() => s.Cancel(), TimeSpan.FromMilliseconds(10));
          Dispatcher.UIThread.MainLoop(s.Token);

          //skip element if is from a linked file and setting is off
          if (!receiveLinkedModels && @base["isRevitLinkedModel"] != null && bool.Parse(@base["isRevitLinkedModel"].ToString()))
            continue;

          var convRes = converter.ConvertToNative(@base);
          RefreshView(UIDoc);

          switch (convRes)
          {
            case ApplicationObject o:
              if (o.Converted.Cast<Element>().ToList() is List<Element> typedList && typedList.Count >= 1)
              {
                convertedObjectsCache.AddConvertedObjects(@base, typedList);
              }
              obj.Update(status: o.Status, createdIds: o.CreatedIds, converted: o.Converted, log: o.Log);
              progress.Report.UpdateReportObject(obj);
              break;
            default:
              break;
          }
        }
        catch (Exception e)
        {
          SpeckleLog.Logger.Warning("Failed to convert ");
          obj.Update(status: ApplicationObject.State.Failed, logItem: e.Message);
          progress.Report.UpdateReportObject(obj);
        }
      }

      return convertedObjectsCache;
    }

    private static void RefreshView(UIDocument UIDoc)
    {
      //regenerate the document and then implement a hack to "refresh" the view
      UIDoc.Document.Regenerate();

      // get the active ui view
      var view = UIDoc.ActiveGraphicalView ?? UIDoc.Document.ActiveView;
      if (view is TableView)
      {
        return;
      }

      var uiView = UIDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

      // "refresh" the active view
      uiView.Zoom(1);
    }

    /// <summary>
    /// Traverses the object graph, returning objects to be converted.
    /// </summary>
    /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
    /// <param name="converter">The converter instance, used to define what objects are convertable</param>
    /// <returns>A flattened list of objects to be converted ToNative</returns>
    private static List<ApplicationObject> FlattenCommitObject(Base obj, ISpeckleConverter converter, Dictionary<string, Base> storedObjects)
    {

      ApplicationObject CreateApplicationObject(Base current)
      {
        if (!converter.CanConvertToNative(current)) return null;

        var appObj = new ApplicationObject(current.id, ConnectorRevitUtils.SimplifySpeckleType(current.speckle_type))
        {
          applicationId = current.applicationId,
          Convertible = true
        };
        if (storedObjects.ContainsKey(current.id))
          return null;

        storedObjects.Add(current.id, current);
        return appObj;
      }

      var traverseFunction = DefaultTraversal.CreateRevitTraversalFunc(converter);

      var objectsToConvert = traverseFunction.Traverse(obj)
        .Select(tc => CreateApplicationObject(tc.current))
        .Where(appObject => appObject != null)
        .Reverse()
        .ToList();

      return objectsToConvert;
    }

  }
}
