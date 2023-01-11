using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Avalonia.Threading;
using ConnectorRevit.Revit;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Revit.Async;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.ConnectorRevit.UI
{
  
  public partial class ConnectorBindingsRevit
  {
    public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
    public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    /// 
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      //make sure to instance a new copy so all values are reset correctly
      var converter = (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
      converter.SetContextDocument(CurrentDoc.Document);
      var previouslyReceiveObjects = state.ReceivedObjects;

      // set converter settings as tuples (setting slug, setting selection)
      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      Commit myCommit = null;
      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      if (state.CommitId == "latest")
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        myCommit = res.commits.items.FirstOrDefault();
      }
      else
      {
        myCommit = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
      }

      state.LastSourceApp = myCommit.sourceApplication;

      string referencedObject = myCommit.referencedObject;

      var commitObject = await Operations.Receive(
          referencedObject,
          progress.CancellationTokenSource.Token,
          transport,
          onProgressAction: dict => progress.Update(dict),
          onErrorAction: (s, e) =>
          {
            progress.Report.LogOperationError(e);
            progress.CancellationTokenSource.Cancel();
          },
          onTotalChildrenCountKnown: count => { progress.Max = count; },
          disposeTransports: true
          );

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = myCommit?.id,
          message = myCommit?.message,
          sourceApplication = ConnectorRevitUtils.RevitAppName
        });
      }
      catch
      {
        // Do nothing!
      }

      if (progress.Report.OperationErrorsCount != 0)
        return state;

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      Preview.Clear();
      StoredObjects.Clear();


      Preview = FlattenCommitObject(commitObject, converter);
      foreach (var previewObj in Preview)
        progress.Report.Log(previewObj);

      converter.ReceiveMode = state.ReceiveMode;
      // needs to be set for editing to work 
      converter.SetPreviousContextObjects(previouslyReceiveObjects);
      // needs to be set for openings in floors and roofs to work
      converter.SetContextObjects(Preview);

      try
      {
        await RevitTask.RunAsync(() => UpdateForCustomMapping(state, progress, myCommit.sourceApplication));
      }
      catch (Exception ex)
      {
        progress.Report.LogOperationError(new Exception("Could not update receive object with user types. Using default mapping."));
      }

      await RevitTask.RunAsync(app =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Baking stream {state.StreamId}"))
        {
          var failOpts = t.GetFailureHandlingOptions();
          failOpts.SetFailuresPreprocessor(new ErrorEater(converter));
          failOpts.SetClearAfterRollback(true);
          t.SetFailureHandlingOptions(failOpts);
          t.Start();
          
          var newPlaceholderObjects = ConvertReceivedObjects(converter, progress);
          // receive was cancelled by user
          if (newPlaceholderObjects == null)
          {
            progress.Report.LogOperationError(new Exception("fatal error: receive cancelled by user"));
            t.RollBack();
            return;
          }

          if (state.ReceiveMode == ReceiveMode.Update)
            DeleteObjects(previouslyReceiveObjects, newPlaceholderObjects);

          state.ReceivedObjects = newPlaceholderObjects;

          t.Commit();
        }

      });

      if (converter.Report.OperationErrors.Any(x => x.Message.Contains("fatal error")))
        return null; // the commit is being rolled back

      return state;
    }

    //delete previously sent object that are no more in this stream
    private void DeleteObjects(List<ApplicationObject> previouslyReceiveObjects, List<ApplicationObject> newPlaceholderObjects)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (obj.CreatedIds.Count == 0 || newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        var element = CurrentDoc.Document.GetElement(obj.CreatedIds.FirstOrDefault());
        if (element != null)
          CurrentDoc.Document.Delete(element.Id);
      }
    }

    private List<ApplicationObject> ConvertReceivedObjects(ISpeckleConverter converter, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting;
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;

      foreach (var obj in Preview)
      {
        var @base = StoredObjects[obj.OriginalId];
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          placeholders = null;
          break;
        }

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
      }

      return placeholders;
    }

    private void RefreshView()
    {
      //regenerate the document and then implement a hack to "refresh" the view
      CurrentDoc.Document.Regenerate();

      // get the active ui view
      var view = CurrentDoc.ActiveGraphicalView ?? CurrentDoc.Document.ActiveView;
      var uiView = CurrentDoc.GetOpenUIViews().FirstOrDefault(uv => uv.ViewId.Equals(view.Id));

      // "refresh" the active view
      uiView.Zoom(1);
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
        var appObj = new ApplicationObject(@base.id, ConnectorRevitUtils.SimplifySpeckleType(@base.speckle_type)) { applicationId = @base.applicationId, Status = ApplicationObject.State.Unknown };

        if (converter.CanConvertToNative(@base))
        {
          appObj.Convertible = true;
          objects.Add(appObj);
          StoredObjects.Add(@base.id, @base);
          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
            objects.AddRange(FlattenCommitObject(@base[prop], converter));
          return objects;
        }
      }

      if (obj is List<object> list)
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
          appObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Receiving this object type is not supported in Revit");
          objects.Add(appObj);
        }
      }

      return objects;
    }
  }
}
