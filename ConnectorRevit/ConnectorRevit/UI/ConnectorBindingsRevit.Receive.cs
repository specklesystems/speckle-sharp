using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Avalonia.Threading;
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

namespace Speckle.ConnectorRevit.UI;

public partial class ConnectorBindingsRevit
{
  public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
  public Dictionary<string, Base> StoredObjects = new();

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
    {
      settings.Add(setting.Slug, setting.Selection);
    }

    converter.SetConverterSettings(settings);

    // track object types for mixpanel logging
    Dictionary<string, int> typeCountDict = new();

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
    {
      progress.Report.Log(previewObj);
      if (StoredObjects.TryGetValue(previewObj.OriginalId, out Base previewBaseObj))
      {
        typeCountDict.TryGetValue(previewBaseObj.speckle_type, out var currentCount);
        typeCountDict[previewBaseObj.speckle_type] = ++currentCount;
      }
    }

    // track the object type counts as an event before we try to receive
    // this will tell us the composition of a commit the user is trying to convert and receive, even if it's not successfully converted or received
    // we are capped at 255 properties for mixpanel events, so we need to check dict entries
    var typeCountList = typeCountDict
      .Select(o => new { TypeName = o.Key, Count = o.Value })
      .OrderBy(pair => pair.Count)
      .Reverse()
      .Take(200);

    Analytics.TrackEvent(
      Analytics.Events.ConvertToNative,
      new Dictionary<string, object>() { { "typeCount", typeCountList } }
    );

    converter.ReceiveMode = state.ReceiveMode;
    // needs to be set for editing to work
    var previousObjects = new StreamStateCache(state);
    converter.SetContextDocument(previousObjects);
    // needs to be set for openings in floors and roofs to work
    converter.SetContextObjects(Preview);

    // share the same revit element cache between the connector and converter
    converter.SetContextDocument(revitDocumentAggregateCache);

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
        .Map(
          state.Settings.FirstOrDefault(x => x.Slug == "receive-mappings"),
          state.Settings.FirstOrDefault(x => x.Slug == DsFallbackSlug)
        )
        .ConfigureAwait(false);
    }
    catch (SpeckleException ex)
    {
      SpeckleLog.Logger.Warning("Failed to map incoming types to Revit types. Reason: {ex.Message}", ex);
      StreamViewModel.HandleCommandException(ex, false, "MapIncomingTypesCommand");
    }
    finally
    {
      MainViewModel.CloseDialog();
    }

    await APIContext
      .Run(_ =>
      {
        using var transactionManager = new TransactionManager(state.StreamId, CurrentDoc.Document);
        transactionManager.Start();

        try
        {
          converter.SetContextDocument(transactionManager);

          var convertedObjects = ConvertReceivedObjects(converter, progress, transactionManager);

          if (state.ReceiveMode == ReceiveMode.Update)
          {
            DeleteObjects(previousObjects, convertedObjects, transactionManager);
          }

          previousObjects.AddConvertedElements(convertedObjects);
          transactionManager.Finish();
        }
        catch (OperationCanceledException) when (progress.CancellationToken.IsCancellationRequested)
        {
          transactionManager.RollbackAll();
          throw;
        }
        catch (SpeckleNonUserFacingException ex)
        {
          SpeckleLog.Logger.Error(ex, "Rolling back connector transaction");
          transactionManager.RollbackAll();
          throw;
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException ex)
        {
          SpeckleLog.Logger.Error(ex, "Rolling back connector transaction");
          transactionManager.RollbackAll();
          throw;
        }
        finally
        {
          revitDocumentAggregateCache.InvalidateAll();
          CurrentOperationCancellation = null;
        }
      })
      .ConfigureAwait(false);

    return state;
  }

  //delete previously sent object that are no more in this stream
  private void DeleteObjects(
    IReceivedObjectIdMap<Base, Element> previousObjects,
    IConvertedObjectsCache<Base, Element> convertedObjects,
    TransactionManager transactionManager
  )
  {
    var previousAppIds = previousObjects.GetAllConvertedIds().ToList();
    transactionManager.StartSubtransaction();
    for (var i = previousAppIds.Count - 1; i >= 0; i--)
    {
      var appId = previousAppIds[i];
      if (string.IsNullOrEmpty(appId) || convertedObjects.HasConvertedObjectWithId(appId))
      {
        continue;
      }

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
          catch (Autodesk.Revit.Exceptions.ArgumentException)
          {
            // unable to delete object that was previously received and then removed from the stream
            // because it was already deleted by the user. This isn't an issue and can safely be ignored.
          }
        }

        previousObjects.RemoveConvertedId(appId);
      }
    }

    transactionManager.CommitSubtransaction();
  }

  private IConvertedObjectsCache<Base, Element> ConvertReceivedObjects(
    ISpeckleConverter converter,
    ProgressViewModel progress,
    TransactionManager transactionManager
  )
  {
    // Traverses through the `elements` property of the given base
    void ConvertNestedElements(Base @base, ApplicationObject appObj, bool receiveDirectMesh)
    {
      if (@base == null)
      {
        return;
      }

      var nestedElements = @base["elements"] ?? @base["@elements"];

      if (nestedElements == null)
      {
        return;
      }

      // set host in converter state.
      // assumes host is the first converted object of the appObject
      var host = appObj == null || !appObj.Converted.Any() ? null : appObj.Converted.First() as Element;
      using var ctx = RevitConverterState.Push();
      ctx.CurrentHostElement = host;

      // traverse each element member and convert
      foreach (var obj in GraphTraversal.TraverseMember(nestedElements))
      {
        // create the application object and log to reports
        var nestedAppObj = Preview.Where(o => o.OriginalId == obj.id)?.FirstOrDefault();
        if (nestedAppObj == null)
        {
          nestedAppObj = new ApplicationObject(obj.id, ConnectorRevitUtils.SimplifySpeckleType(obj.speckle_type))
          {
            applicationId = obj.applicationId,
            Convertible = converter.CanConvertToNative(obj)
          };
          progress.Report.Log(nestedAppObj);
          converter.Report.Log(nestedAppObj);
        }

        // convert
        var converted = ConvertObject(nestedAppObj, obj, receiveDirectMesh, converter, progress, transactionManager);

        // Check if parent conversion succeeded before attempting the children
        if (
          receiveDirectMesh || converted?.Status is ApplicationObject.State.Created or ApplicationObject.State.Updated
        )
        {
          // recurse and convert nested elements
          ConvertNestedElements(obj, nestedAppObj, receiveDirectMesh);
        }
      }
    }

    var convertedObjectsCache = new ConvertedObjectsCache();
    converter.SetContextDocument(convertedObjectsCache);

    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 1;

    // Get setting to skip linked model elements if necessary
    var receiveLinkedModelsSetting =
      CurrentSettings?.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting;
    var receiveLinkedModels = receiveLinkedModelsSetting?.IsChecked ?? false;

    var receiveDirectMesh = false;
    var fallbackToDirectShape = false;
    var directShapeStrategySetting =
      CurrentSettings?.FirstOrDefault(x => x.Slug == "direct-shape-strategy") as ListBoxSetting;
    switch (directShapeStrategySetting!.Selection)
    {
      case "Always":
        receiveDirectMesh = true;
        break;
      case "On Error":
        fallbackToDirectShape = true;
        break;
      case "Never":
      case null:
        // Do nothing, default values will do.
        break;
    }

    using var d0 = LogContext.PushProperty("converterName", converter.Name);
    using var d1 = LogContext.PushProperty("converterAuthor", converter.Author);
    using var d2 = LogContext.PushProperty("conversionDirection", nameof(ISpeckleConverter.ConvertToNative));
    using var d4 = LogContext.PushProperty("converterReceiveMode", converter.ReceiveMode);

    // convert
    var index = -1;
    while (++index < Preview.Count)
    {
      var obj = Preview[index];
      progress.CancellationToken.ThrowIfCancellationRequested();

      var @base = StoredObjects[obj.OriginalId];

      // skip if this object has already been converted from a nested elements loop
      if (obj.Status != ApplicationObject.State.Unknown)
      {
        continue;
      }

      conversionProgressDict["Conversion"]++;
      progress.Update(conversionProgressDict);

      //skip element if is from a linked file and setting is off
      if (
        !receiveLinkedModels
        && @base["isRevitLinkedModel"] != null
        && bool.Parse(@base["isRevitLinkedModel"].ToString())
      )
      {
        continue;
      }

      var converted = ConvertObject(obj, @base, receiveDirectMesh, converter, progress, transactionManager);
      // Determine if we should use the fallback DirectShape conversion
      // Should only happen when receiveDirectMesh is OFF, fallback is ON and object failed normal conversion.
      bool usingFallback =
        !receiveDirectMesh && fallbackToDirectShape && converted.Status == ApplicationObject.State.Failed;
      if (usingFallback)
      {
        obj.Log.Add("Conversion to native Revit object failed. Retrying conversion with displayable geometry.");
        converted = ConvertObject(obj, @base, true, converter, progress, transactionManager);
        if (converted == null)
        {
          obj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null.");
        }
      }

      RefreshView();
      if (index % 50 == 0)
      {
        transactionManager.Commit();
        transactionManager.Start();
      }

      // Check if parent conversion succeeded or fallback is enabled before attempting the children
      if (
        usingFallback
        || receiveDirectMesh
        || converted?.Status is ApplicationObject.State.Created or ApplicationObject.State.Updated
      )
      {
        // continue traversing for hosted elements
        // use DirectShape conversion if the parent was converted using fallback or if the global setting is active.
        ConvertNestedElements(@base, converted, usingFallback || receiveDirectMesh);
      }
    }

    return convertedObjectsCache;
  }

  private ApplicationObject ConvertObject(
    ApplicationObject obj,
    Base @base,
    bool receiveDirectMesh,
    ISpeckleConverter converter,
    ProgressViewModel progress,
    TransactionManager transactionManager
  )
  {
    progress.CancellationToken.ThrowIfCancellationRequested();

    if (obj == null || @base == null)
    {
      return obj;
    }

    using var _d3 = LogContext.PushProperty("speckleType", @base.speckle_type);
    transactionManager.StartSubtransaction();

    try
    {
      var s = new CancellationTokenSource();
      DispatcherTimer.RunOnce(() => s.Cancel(), TimeSpan.FromMilliseconds(10));
      Dispatcher.UIThread.MainLoop(s.Token);

      ApplicationObject convRes;
      if (converter.CanConvertToNative(@base))
      {
        if (receiveDirectMesh)
        {
          convRes = converter.ConvertToNativeDisplayable(@base) as ApplicationObject;
          if (convRes == null)
          {
            obj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null.");
            return obj;
          }
        }
        else
        {
          convRes = converter.ConvertToNative(@base) as ApplicationObject;
          if (convRes == null || convRes.Status == ApplicationObject.State.Failed)
          {
            var logItem =
              convRes == null
                ? "Conversion returned null"
                : "Conversion failed with errors: " + string.Join("/n", convRes.Log);
            obj.Update(status: ApplicationObject.State.Failed, logItem: logItem);
            return obj;
          }
        }
      }
      else if (converter.CanConvertToNativeDisplayable(@base))
      {
        obj.Log.Add("No direct conversion exists. Converting displayable geometry.");
        convRes = converter.ConvertToNativeDisplayable(@base) as ApplicationObject;
        if (convRes == null)
        {
          obj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null.");
          return obj;
        }
      }
      else
      {
        obj.Update(
          status: ApplicationObject.State.Skipped,
          logItem: "No direct conversion or displayable values can be converted."
        );
        return obj;
      }

      obj.Update(
        status: convRes.Status,
        createdIds: convRes.CreatedIds,
        converted: convRes.Converted,
        log: convRes.Log
      );

      progress.Report.UpdateReportObject(obj);
      RefreshView();
      transactionManager.CommitSubtransaction();
    }
    catch (ConversionNotReadyException ex)
    {
      transactionManager.RollbackSubTransaction();
      var notReadyDataCache = revitDocumentAggregateCache.GetOrInitializeEmptyCacheOfType<ConversionNotReadyCacheData>(
        out _
      );
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
    catch (Exception ex) when (!ex.IsFatal())
    {
      transactionManager.RollbackSubTransaction();
      SpeckleLog.Logger.Warning(ex, "Failed to convert due to unexpected error.");
      obj.Update(status: ApplicationObject.State.Failed, logItem: "Failed to convert due to unexpected error.");
      obj.Log.Add($"{ex.Message}");
      progress.Report.UpdateReportObject(obj);
    }

    return obj;
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
      {
        return null;
      }

      if (!converter.CanConvertToNative(current) && !isDisplayable)
      {
        return null;
      }

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
