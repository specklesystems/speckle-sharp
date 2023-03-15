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
using Serilog;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings

  {
    #region receiving
    public override bool CanPreviewReceive => false;
    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      return null;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      Exceptions.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
      converter.SetContextDocument(Model);
      //var previouslyRecieveObjects = state.ReceivedObjects;

      var settings = new Dictionary<string, string>();
      CurrentSettings = state.Settings;
      foreach (var setting in state.Settings)
        settings.Add(setting.Slug, setting.Selection);
      converter.SetConverterSettings(settings);

      Exceptions.Clear();

      Commit myCommit = await ConnectorHelpers.GetCommitFromState(progress.CancellationToken, state);
      state.LastCommit = myCommit;
      Base commitObject = await ConnectorHelpers.ReceiveCommit(myCommit, state, progress);
      await ConnectorHelpers.TryCommitReceived(progress.CancellationToken, state, myCommit, ConnectorTeklaStructuresUtils.TeklaStructuresAppName);
      
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;
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
