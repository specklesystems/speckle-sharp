using Avalonia.Threading;
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
using System.Threading;
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
      //var converter = new ConverterCSI();
      var appName = GetHostAppVersion(Model);
      var converter = kit.LoadConverter(appName);
      converter.SetContextDocument(Model);
      Exceptions.Clear();
      var previouslyReceivedObjects = state.ReceivedObjects;

      if (converter == null)
      {
        throw new Exception("Could not find any Kit!");
        //RaiseNotification($"Could not find any Kit!");
        progress.CancellationTokenSource.Cancel();
        //return null;
      }

      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      Exceptions.Clear();

      Commit commit = null;
      if (state.CommitId == "latest")
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        commit = res.commits.items.FirstOrDefault();
      }
      else
      {
        commit = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
      }
      string referencedObject = commit.referencedObject;

      state.LastSourceApp = commit.sourceApplication;

      var commitObject = await Operations.Receive(
                referencedObject,
                progress.CancellationTokenSource.Token,
                transport,
                onProgressAction: dict => progress.Update(dict),
                onErrorAction: (Action<string, Exception>)((s, e) =>
                {
                  progress.Report.LogOperationError(e);
                  progress.CancellationTokenSource.Cancel();
                }),
                 onTotalChildrenCountKnown: count => { progress.Max = count; },
                disposeTransports: true
                );

      if (progress.Report.OperationErrorsCount != 0)
        return state;

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = GetHostAppVersion(Model)
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
      //converter.SetPreviousContextObjects(previouslyReceivedObjects);

      var newPlaceholderObjects = ConvertReceivedObjects(converter, progress);
      // receive was cancelled by user
      if (newPlaceholderObjects == null)
      {
        progress.Report.LogOperationError(new Exception("fatal error: receive cancelled by user"));
        return null;
      }

      try
      {
        //await state.RefreshStream();
        WriteStateToFile();
      }
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
        WriteStateToFile();
        //state.Errors.Add(e);
        //Globals.Notify($"Receiving done, but failed to update stream from server.\n{e.Message}");
      }
      progress.Report.Merge(converter.Report);
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
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          placeholders = null;
          break;
        }

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

        Model.View.RefreshWindow();

        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);

        //var s = new CancellationTokenSource();
        //DispatcherTimer.RunOnce(() => s.Cancel(), TimeSpan.FromMilliseconds(10));
        //Dispatcher.UIThread.MainLoop(s.Token);
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
  }
}