using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Avalonia.Threading;
using ConnectorRevit.Revit;
using ConnectorRevit.Storage;
using ConnectorRevit.TypeMapping;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using RevitSharedResources.Models;
using Serilog.Context;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Speckle.ConnectorRevit.UI
{

  public partial class ConnectorBindingsRevit
  {
    public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
    public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

    public CancellationTokenSource CurrentOperationCancellation { get; set; }
    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    ///
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      CurrentOperationCancellation = progress.CancellationTokenSource;
      //make sure to instance a new copy so all values are reset correctly
      var converter = (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
      converter.SetContextDocument(CurrentDoc.Document);

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      Commit myCommit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
      state.LastCommit = myCommit;
      Base commitObject = await ConnectorHelpers.ReceiveCommit(myCommit, state, progress);
      await ConnectorHelpers.TryCommitReceived(state, myCommit, ConnectorRevitUtils.RevitAppName, progress.CancellationToken);

      Preview.Clear();
      StoredObjects.Clear();

      Preview = FlattenCommitObject(commitObject, converter);
      foreach (var previewObj in Preview)
        progress.Report.Log(previewObj);


      converter.ReceiveMode = state.ReceiveMode;
      // needs to be set for editing to work
      var previousObjects = new StreamStateCache(state);
      converter.SetContextDocument(previousObjects);
      // needs to be set for openings in floors and roofs to work
      converter.SetContextObjects(Preview);

      // share the same revit element cache between the connector and converter
      converter.SetContextDocument(revitDocumentAggregateCache);

//#pragma warning disable CA1031 // Do not catch general exception types
//      try
//      {
//        var elementTypeMapper = new ElementTypeMapper(converter, revitDocumentAggregateCache, Preview, StoredObjects, CurrentDoc.Document);
//        await elementTypeMapper.Map(state.Settings.FirstOrDefault(x => x.Slug == "receive-mappings"))
//          .ConfigureAwait(false);
//      }
//      catch (Exception ex)
//      {
//        var speckleEx = new SpeckleException($"Failed to map incoming types to Revit types. Reason: {ex.Message}", ex);
//        StreamViewModel.HandleCommandException(speckleEx, false, "MapIncomingTypesCommand");
//        progress.Report.LogOperationError(new Exception("Could not update receive object with user types. Using default mapping.", ex));
//      }
//      finally
//      {
//        MainViewModel.CloseDialog();
//      }
//#pragma warning restore CA1031 // Do not catch general exception types

      var (success, exception) = await APIContext.Run(_ =>
      {
        using ITransactionManager transactionManager = new TransactionManager(state.StreamId, CurrentDoc.Document);
        transactionManager.Start();

        try
        {
          converter.SetContextDocument(transactionManager);

          var convertedObjects = ConvertReceivedObjects(converter, progress, transactionManager);

          if (state.ReceiveMode == ReceiveMode.Update)
            DeleteObjects(previousObjects, convertedObjects);

          previousObjects.AddConvertedElements(convertedObjects);
          transactionManager.Finish();
          return (true, null);
        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Error(ex, "Rolling back connector transaction");

          string message = $"Fatal Error: {ex.Message}";
          if (ex is OperationCanceledException) message = "Receive cancelled";
          progress.Report.LogOperationError(new Exception($"{message} - Changes have been rolled back", ex));

          transactionManager.RollbackAll();
          return (false, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
        }
      }).ConfigureAwait(false);

      revitDocumentAggregateCache.InvalidateAll();
      CurrentOperationCancellation = null;

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

      return state;
    }

    //delete previously sent object that are no more in this stream
    private void DeleteObjects(IReceivedObjectIdMap<Base, Element> previousObjects, IConvertedObjectsCache<Base, Element> convertedObjects)
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
          var elementToDelete = CurrentDoc.Document.GetElement(elementId);

          if (elementToDelete != null && !elementToDelete.Pinned && elementToDelete.IsValidObject)
          {
            try
            {
              CurrentDoc.Document.Delete(elementToDelete.Id);
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

    private IConvertedObjectsCache<Base, Element> ConvertReceivedObjects(ISpeckleConverter converter, ProgressViewModel progress, ITransactionManager transactionManager)
    {
      using var _d0 = LogContext.PushProperty("converterName", converter.Name);
      using var _d1 = LogContext.PushProperty("converterAuthor", converter.Author);
      using var _d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToNative));


      var convertedObjectsCache = new ConvertedObjectsCache();
      converter.SetContextDocument(convertedObjectsCache);

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting;
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;

      var index = -1;
      while (++index < Preview.Count)
      {
        var obj = Preview[index];
        progress.CancellationToken.ThrowIfCancellationRequested();

        var @base = StoredObjects[obj.OriginalId];

        using var _d3 = LogContext.PushProperty("speckleType", @base.speckle_type);
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

          //var lowerType = @base.speckle_type.ToLower();
          //var cat = (@base["category"] as string)?.ToLower() ?? "";
          ////if (!lowerType.Contains("instance") 
          ////  || lowerType.Contains("mepfamily")
          ////  || cat.Contains("generic")
          ////  || cat.Contains("structural")
          ////  || cat.Contains("casework")
          ////  || cat.Contains("site"))
          ////  continue;
          //if (!lowerType.Contains("instance") 
          //  || !cat.Contains("casework"))
          //  continue;
          //Trace.WriteLine(lowerType, cat);

          transactionManager.StartSubtransaction();
          var convRes = converter.ConvertToNative(@base);
          transactionManager.CommitSubtransaction();
          RefreshView();

          switch (convRes)
          {
            case ApplicationObject o:
              obj.Update(status: o.Status, createdIds: o.CreatedIds, converted: o.Converted, log: o.Log);
              progress.Report.UpdateReportObject(obj);
              break;
            default:
              break;
          }
        }
        catch (ConversionNotReadyException ex) 
        {
          var notReadyDataCache = revitDocumentAggregateCache
            .GetOrInitializeEmptyCacheOfType<ConversionNotReadyCacheData>(out _);
          var notReadyData = notReadyDataCache
            .GetOrAdd(@base.id, () => new ConversionNotReadyCacheData(), out _);

          if (++notReadyData.NumberOfTimesCaught > 2)
          {
            SpeckleLog.Logger.Warning(ex, $"Speckle object of type {@base.GetType()} was waiting for an object to convert that never did");
            obj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
            progress.Report.UpdateReportObject(obj);
          }
          else
          {
            Preview.Add(obj);
          }
          // the struct must be saved to the cache again or the "numberOfTimesCaught" increment will not persist
          notReadyDataCache.Set(@base.id, notReadyData);
        }
        catch (Exception ex)
        {
          SpeckleLog.Logger.Warning(ex, "Failed to convert");
          obj.Update(status: ApplicationObject.State.Failed, logItem: ex.Message);
          progress.Report.UpdateReportObject(obj);
        }
      }

      return convertedObjectsCache;
    }

    private void RefreshView()
    {
      //regenerate the document and then implement a hack to "refresh" the view
      CurrentDoc.Document.Regenerate();

      // get the active ui view
      var view = CurrentDoc.ActiveGraphicalView ?? CurrentDoc.Document.ActiveView;
      if (view is TableView)
      {
        return;
      }

      var uiView = CurrentDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

      // "refresh" the active view
      uiView.Zoom(1);
    }

    /// <summary>
    /// Traverses the object graph, returning objects to be converted.
    /// </summary>
    /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
    /// <param name="converter">The converter instance, used to define what objects are convertable</param>
    /// <returns>A flattened list of objects to be converted ToNative</returns>
    private List<ApplicationObject> FlattenCommitObject(Base obj, ISpeckleConverter converter)
    {

      ApplicationObject CreateApplicationObject(Base current)
      {
        if (!converter.CanConvertToNative(current)) return null;

        var appObj = new ApplicationObject(current.id, ConnectorRevitUtils.SimplifySpeckleType(current.speckle_type))
        {
          applicationId = current.applicationId,
          Convertible = true
        };
        if (StoredObjects.ContainsKey(current.id))
          return null;

        StoredObjects.Add(current.id, current);
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
