using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using Revit.Async;
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
      await ConnectorHelpers.TryCommitReceived(
        state,
        myCommit,
        ConnectorRevitUtils.RevitAppName,
        progress.CancellationToken
      );

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

#pragma warning disable CA1031 // Do not catch general exception types
      try
      {
        var elementTypeMapper = new ElementTypeMapper(
          converter,
          revitDocumentAggregateCache,
          Preview,
          StoredObjects,
          CurrentDoc.Document
        );
        await elementTypeMapper
          .Map(state.Settings.FirstOrDefault(x => x.Slug == "receive-mappings"))
          .ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        var speckleEx = new SpeckleException($"Failed to map incoming types to Revit types. Reason: {ex.Message}", ex);
        StreamViewModel.HandleCommandException(speckleEx, false, "MapIncomingTypesCommand");
        progress.Report.LogOperationError(
          new Exception("Could not update receive object with user types. Using default mapping.", ex)
        );
      }
      finally
      {
        MainViewModel.CloseDialog();
      }
#pragma warning restore CA1031 // Do not catch general exception types

      var (success, exception) = await RevitTask
        .RunAsync(app =>
        {
          string transactionName = $"Baking stream {state.StreamId}";
          using var g = new TransactionGroup(CurrentDoc.Document, transactionName);
          using var t = new Transaction(CurrentDoc.Document, transactionName);

          g.Start();
          var failOpts = t.GetFailureHandlingOptions();
          var errorEater = new ErrorEater(converter);
          failOpts.SetFailuresPreprocessor(errorEater);
          failOpts.SetClearAfterRollback(true);
          t.SetFailureHandlingOptions(failOpts);
          t.Start();

          try
          {
            converter.SetContextDocument(t);

            var convertedObjects = ConvertReceivedObjects(converter, progress, settings);

            if (state.ReceiveMode == ReceiveMode.Update)
              DeleteObjects(previousObjects, convertedObjects);

            previousObjects.AddConvertedElements(convertedObjects);
            t.Commit();

            if (t.GetStatus() == TransactionStatus.RolledBack)
            {
              var numTotalErrors = errorEater.CommitErrorsDict.Sum(kvp => kvp.Value);
              var numUniqueErrors = errorEater.CommitErrorsDict.Keys.Count;

              var exception = errorEater.GetException();
              if (exception == null)
                SpeckleLog.Logger.Warning(
                  "Revit commit failed with {numUniqueErrors} unique errors and {numTotalErrors} total errors, but the ErrorEater did not capture any exceptions",
                  numUniqueErrors,
                  numTotalErrors
                );
              else
                SpeckleLog.Logger.Fatal(
                  exception,
                  "The Revit API could not resolve {numUniqueErrors} unique errors and {numTotalErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back.",
                  numUniqueErrors,
                  numTotalErrors
                );

              return (
                false,
                exception
                  ?? new SpeckleException(
                    $"The Revit API could not resolve {numUniqueErrors} unique errors and {numTotalErrors} total errors when trying to commit the Speckle model. The whole transaction is being rolled back."
                  )
              );
            }

            g.Assimilate();
            return (true, null);
          }
          catch (Exception ex)
          {
            SpeckleLog.Logger.Error(
              ex,
              "Rolling back connector transaction {transactionName} {transactionType}",
              transactionName,
              t.GetType()
            );

            string message = $"Fatal Error: {ex.Message}";
            if (ex is OperationCanceledException)
              message = "Receive cancelled";
            progress.Report.LogOperationError(new Exception($"{message} - Changes have been rolled back", ex));

            t.RollBack();
            g.RollBack();
            return (false, ex); //We can't throw exceptions in from RevitTask, but we can return it along with a success status
          }
        })
        .ConfigureAwait(false);

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
    private void DeleteObjects(
      IReceivedObjectIdMap<Base, Element> previousObjects,
      IConvertedObjectsCache<Base, Element> convertedObjects
    )
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

    private IConvertedObjectsCache<Base, Element> ConvertReceivedObjects(
      ISpeckleConverter converter,
      ProgressViewModel progress,
      Dictionary<string, string> settings
    )
    {
      // Retries a conversion using the direct mesh setting.
      // Used in the case of failed conversions
      ApplicationObject RetryConversionAsDisplayable(Base @base, Dictionary<string, string> fallbackSettings)
      {
        converter.SetConverterSettings(fallbackSettings);
        var convRes = converter.ConvertToNative(@base) as ApplicationObject;
        if (convRes != null)
          RefreshView();
        converter.SetConverterSettings(settings);
        return convRes;
      }

      using var _d0 = LogContext.PushProperty("converterName", converter.Name);
      using var _d1 = LogContext.PushProperty("converterAuthor", converter.Author);
      using var _d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToNative));

      var convertedObjectsCache = new ConvertedObjectsCache();
      converter.SetContextDocument(convertedObjectsCache);

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting =
        CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting;
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;

      // Get direct mesh setting and create modified settings in case this is used for retried conversions
      var receiveDirectMeshSetting =
        CurrentSettings.FirstOrDefault(x => x.Slug == "recieve-objects-mesh") as CheckBoxSetting;
      var receiveDirectMesh = receiveDirectMeshSetting != null ? receiveDirectMeshSetting.IsChecked : false;
      var fallbackSettings = new Dictionary<string, string>();
      foreach (var setting in settings)
      {
        var value = setting.Key == "recieve-objects-mesh" ? true.ToString() : setting.Value;
        fallbackSettings.Add(setting.Key, setting.Value);
      }

      // convert
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
          if (
            !receiveLinkedModels
            && @base["isRevitLinkedModel"] != null
            && bool.Parse(@base["isRevitLinkedModel"].ToString())
          )
            continue;

          var shouldConvertAsDisplayable = !obj.Convertible;
          var convRes = shouldConvertAsDisplayable
            ? RetryConversionAsDisplayable(@base, fallbackSettings)
            : converter.ConvertToNative(@base) as ApplicationObject;

          if (convRes != null)
          {
            obj.Update(
              status: convRes.Status,
              createdIds: convRes.CreatedIds,
              converted: convRes.Converted,
              log: convRes.Log
            );
            if (
              convRes.Status == ApplicationObject.State.Failed
              && !receiveDirectMesh
              && DefaultTraversal.HasDisplayValue(@base)
            )
            {
              shouldConvertAsDisplayable = true;
            }
            else
            {
              RefreshView();
            }
          }
          else if (!shouldConvertAsDisplayable && !receiveDirectMesh && DefaultTraversal.HasDisplayValue(@base))
          {
            shouldConvertAsDisplayable = true;
          }

          // if the conversion status failed, reconvert as directShape if possible
          if (shouldConvertAsDisplayable)
          {
            obj.Log.Add($"First conversion attempt failed. Reconverting as direct shape.");
            convRes = RetryConversionAsDisplayable(@base, fallbackSettings);
          }

          if (convRes != null) // TODO: should this really be returning null ever?
          {
            obj.Update(
              status: convRes.Status,
              createdIds: convRes.CreatedIds,
              converted: convRes.Converted,
              log: convRes.Log
            );
          }
          else
          {
            obj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null");
          }

          progress.Report.UpdateReportObject(obj);
        }
        catch (ConversionNotReadyException ex)
        {
          var notReadyDataCache =
            revitDocumentAggregateCache.GetOrInitializeEmptyCacheOfType<ConversionNotReadyCacheData>(out _);
          var notReadyData = notReadyDataCache.GetOrAdd(@base.id, () => new ConversionNotReadyCacheData(), out _);

          if (++notReadyData.NumberOfTimesCaught > 2)
          {
            SpeckleLog.Logger.Warning(
              ex,
              $"Speckle object of type {@base.GetType()} was waiting for an object to convert that never did"
            );
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
          obj.Log.Add($"First conversion attempt failed: {ex.Message}");

          // reconvert as directShape if possible
          if (!receiveDirectMesh && DefaultTraversal.HasDisplayValue(@base))
          {
            var convRes = RetryConversionAsDisplayable(@base, fallbackSettings);
            if (convRes != null)
            {
              obj.Update(
                status: convRes.Status,
                createdIds: convRes.CreatedIds,
                converted: convRes.Converted,
                log: convRes.Log
              );
            }
            else
            {
              obj.Update(status: ApplicationObject.State.Failed, logItem: "Reconversion returned null");
            }
          }
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
        // determine if this object is displayable
        var isDisplayable = DefaultTraversal.displayValuePropAliases.Any(o => current[o] != null);

        // skip if this object was already stored, if it's not convertible and has no displayables
        if (StoredObjects.ContainsKey(current.id))
          return null;
        if (!converter.CanConvertToNative(current) && !isDisplayable)
          return null;

        // create application object and store
        var appObj = new ApplicationObject(current.id, ConnectorRevitUtils.SimplifySpeckleType(current.speckle_type))
        {
          applicationId = current.applicationId,
          Convertible = converter.CanConvertToNative(current)
        };
        StoredObjects.Add(current.id, current);
        return appObj;
      }

      var traverseFunction = DefaultTraversal.CreateRevitTraversalFunc(converter);

      var objectsToConvert = traverseFunction
        .Traverse(obj)
        .Select(tc => CreateApplicationObject(tc.current))
        .Where(appObject => appObject != null)
        .Reverse()
        .ToList();

      return objectsToConvert;
    }
  }
}
