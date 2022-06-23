using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorTeklaStructures.Util;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>();
    }
    #region receiving
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      Exceptions.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
      converter.SetContextDocument(Model);
      //var previouslyRecieveObjects = state.ReceivedObjects;

      if (converter == null)
      {
        throw new Exception("Could not find any Kit!");
        //RaiseNotification($"Could not find any Kit!");
        progress.CancellationTokenSource.Cancel();
        //return null;
      }


      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

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
                //onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count),
                disposeTransports: true
                );

      if (progress.Report.OperationErrorsCount != 0)
      {
        return state;
      }

      try
      {
        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = ConnectorTeklaStructuresUtils.TeklaStructuresAppName
        });
      }
      catch
      {
        // Do nothing!
      }


      if (progress.Report.OperationErrorsCount != 0)
      {
        return state;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      //Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);
      };


      var commitObjs = FlattenCommitObject(commitObject, converter);
      foreach (var commitObj in commitObjs)
      {
        BakeObject(commitObj, state, converter);
        updateProgressAction?.Invoke();
      }


      Model.CommitChanges();
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






    /// <summary>
    /// conversion to native
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="state"></param>
    /// <param name="converter"></param>
    private void BakeObject(Base obj, StreamState state, ISpeckleConverter converter)
    {
      try
      {
        converter.ConvertToNative(obj);
      }
      catch (Exception e)
      {
        var exception = new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}\n with error\n{e}");
        converter.Report.LogOperationError(exception);
        return;
      }
    }

    /// <summary>
    /// Recurses through the commit object and flattens it. 
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="converter"></param>
    /// <returns></returns>
    private List<Base> FlattenCommitObject(object obj, ISpeckleConverter converter)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (converter.CanConvertToNative(@base))
        {
          objects.Add(@base);

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], converter));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, converter));
        }
        return objects;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, converter));
        }
        return objects;
      }

      return objects;
    }

    #endregion
  }
}
