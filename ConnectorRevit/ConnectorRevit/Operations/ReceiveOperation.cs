using RevitSharedResources.Interfaces;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using Speckle.Core.Kits;
using ConnectorRevit.TypeMapping;
using DesktopUI2.ViewModels;
using Revit.Async;
using Speckle.Core.Logging;
using System.Collections.Generic;
using System;
using Autodesk.Revit.UI;
using DesktopUI2.Models.Settings;
using Speckle.Core.Models.GraphTraversal;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Speckle.ConnectorRevit;
using Avalonia.Threading;
using ConnectorRevit.Services;

namespace ConnectorRevit.Operations
{
  /// <summary>
  /// Creates and executes the roadmap for receiving a Speckle commit into a Revit document.
  /// This is meant to be a transient service meaning that each receive operation that the users does
  /// should instantiate a new instance of the <see cref="ReceiveOperation"/>
  /// </summary>
  public class ReceiveOperation
  {
    private ISpeckleConverter converter;
    private ISpeckleObjectReceiver commitReceiver;
    private IConvertedObjectsCache<Base, Element> convertedObjectsCache;
    private IReceivedObjectIdMap<Base, Element> receivedObjectIdMap;
    private IRevitTransactionManager transactionManager;
    private StreamState state;
    private ProgressViewModel progress;
    private UIDocument uiDocument;

    public ReceiveOperation(
      ISpeckleConverter speckleConverter,
      ISpeckleObjectReceiver speckleObjectReceiver,
      IConvertedObjectsCache<Base, Element> convertedObjectsCache,
      IReceivedObjectIdMap<Base, Element> receivedObjectIdMap,
      IRevitTransactionManager transactionManager,
      IEntityProvider<StreamState> streamStateProvider,
      IEntityProvider<ProgressViewModel> progressProvider,
      IEntityProvider<UIDocument> uiDocumentProvider
    )
    {
      this.converter = speckleConverter;
      this.commitReceiver = speckleObjectReceiver;
      this.convertedObjectsCache = convertedObjectsCache;
      this.receivedObjectIdMap = receivedObjectIdMap;
      this.transactionManager = transactionManager;
      this.state = streamStateProvider.Entity;
      this.progress = progressProvider.Entity;
      this.uiDocument = uiDocumentProvider.Entity;
    }

    public async Task Receive()
    {
      var commitObject = await commitReceiver.ReceiveCommitObject(state, progress).ConfigureAwait(false);

      Dictionary<string, Base> storedObjects = new();
      var preview = FlattenCommitObject(commitObject, storedObjects);
      foreach (var previewObj in preview)
        progress.Report.Log(previewObj);

#pragma warning disable CA1031 // Do not catch general exception types
      try
      {
        var elementTypeMapper = new ElementTypeMapper(converter, preview, storedObjects, uiDocument.Document);
        await elementTypeMapper.Map(state.Settings.FirstOrDefault(x => x.Slug == "receive-mappings"))
          .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        var speckleEx = new SpeckleException($"Failed to map incoming types to Revit types. Reason: {ex.Message}", ex);
        StreamViewModel.HandleCommandException(speckleEx, false, "MapIncomingTypesCommand");
        progress.Report.LogOperationError(new Exception("Could not update receive object with user types. Using default mapping.", ex));
      }
      finally
      {
        MainViewModel.CloseDialog();
      }
#pragma warning restore CA1031 // Do not catch general exception types

      var (success, exception) = await RevitTask.RunAsync(app =>
      {
        transactionManager.Start(state.StreamId, uiDocument.Document);

        try
        {
          ConvertReceivedObjects(preview, storedObjects);

          if (state.ReceiveMode == ReceiveMode.Update)
            DeleteObjects(receivedObjectIdMap, convertedObjectsCache);

          receivedObjectIdMap.AddConvertedElements(convertedObjectsCache);

          transactionManager.Finish();
          return (true, null);
        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Error(ex, "Rolling back connector transaction {streamId} {commidId}", state.StreamId, state.CommitId);

          string message = $"Fatal Error: {ex.Message}";
          if (ex is OperationCanceledException) message = "Receive cancelled";
          progress.Report.LogOperationError(new Exception($"{message} - Changes have been rolled back", ex));

          transactionManager.Rollback();
          return (false, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
        }
      }).ConfigureAwait(false);

      if (!success)
      {
        switch (exception)
        {
          case OperationCanceledException when progress.CancellationToken.IsCancellationRequested:
          case SpeckleNonUserFacingException:
            throw exception;
          default:
            throw new SpeckleException(exception.Message, exception);
        }
      }
    }

    //delete previously sent object that are no more in this stream
    private void DeleteObjects(IReceivedObjectIdMap<Base, Element> previousObjects, IConvertedObjectsCache<Base, Element> convertedObjects)
    {
      var previousAppIds = previousObjects.GetAllConvertedIds().ToList();
      for (var i = previousAppIds.Count - 1; i >= 0; i--)
      {
        var appId = previousAppIds[i];
        if (string.IsNullOrEmpty(appId) || convertedObjects.HasConvertedObjectWithId(appId))
          continue;

        var elementIdToDelete = previousObjects.GetCreatedIdsFromConvertedId(appId);

        foreach (var elementId in elementIdToDelete)
        {
          var elementToDelete = uiDocument.Document.GetElement(elementId);

          if (elementToDelete != null && !elementToDelete.Pinned && elementToDelete.IsValidObject)
          {
            try
            {
              uiDocument.Document.Delete(elementToDelete.Id);
            }
            catch
            {
              // unable to delete previously recieved object
            }
          }

          previousObjects.RemoveConvertedId(appId);
        }
      }
    }

    private void ConvertReceivedObjects(List<ApplicationObject> preview, Dictionary<string, Base> storedObjects)
    {
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = state.Settings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting;
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
          RefreshView();

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
    }

    private void RefreshView()
    {
      //regenerate the document and then implement a hack to "refresh" the view
      uiDocument.Document.Regenerate();

      // get the active ui view
      var view = uiDocument.ActiveGraphicalView ?? uiDocument.Document.ActiveView;
      if (view is TableView || view is null)
      {
        return;
      }

      var uiView = uiDocument.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

      // "refresh" the active view
      uiView?.Zoom(1);
    }

    /// <summary>
    /// Traverses the object graph, returning objects to be converted.
    /// </summary>
    /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
    /// <param name="converter">The converter instance, used to define what objects are convertable</param>
    /// <returns>A flattened list of objects to be converted ToNative</returns>
    private List<ApplicationObject> FlattenCommitObject(Base obj, Dictionary<string, Base> storedObjects)
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
