using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
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
  public partial class ConnectorBindingsRevit2
  {
    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
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
      {
        return null;
      }

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
      {
        return state;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
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
          var flattenedObjects = FlattenCommitObject(commitObject, converter);
          converter.ReceiveMode = state.ReceiveMode;
          // needs to be set for editing to work 
          converter.SetPreviousContextObjects(previouslyReceiveObjects);
          // needs to be set for openings in floors and roofs to work
          converter.SetContextObjects(flattenedObjects.Select(x => new ApplicationPlaceholderObject { applicationId = x.applicationId, NativeObject = x }).ToList());
          var newPlaceholderObjects = ConvertReceivedObjects(flattenedObjects, converter, state, progress);
          // receive was cancelled by user
          if (newPlaceholderObjects == null)
          {
            progress.Report.LogConversionError(new Exception("fatal error: receive cancelled by user"));
            t.RollBack();
            return;
          }

          if (state.ReceiveMode == ReceiveMode.Update)
            DeleteObjects(previouslyReceiveObjects, newPlaceholderObjects);

          state.ReceivedObjects = newPlaceholderObjects;

          t.Commit();
          progress.Report.Merge(converter.Report);
        }

      });

      if (converter.Report.ConversionErrors.Any(x => x.Message.Contains("fatal error")))
      {
        // the commit is being rolled back
        return null;
      }

      return state;
    }

    //delete previously sent object that are no more in this stream
    private void DeleteObjects(List<ApplicationPlaceholderObject> previouslyReceiveObjects, List<ApplicationPlaceholderObject> newPlaceholderObjects)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        var element = CurrentDoc.Document.GetElement(obj.ApplicationGeneratedId);
        if (element != null)
        {
          CurrentDoc.Document.Delete(element.Id);
        }
      }
    }

    private List<ApplicationPlaceholderObject> ConvertReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state, ProgressViewModel progress)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      // Get setting to skip linked model elements if necessary
      var receiveLinkedModelsSetting = (CurrentSettings.FirstOrDefault(x => x.Slug == "linkedmodels-receive") as CheckBoxSetting);
      var receiveLinkedModels = receiveLinkedModelsSetting != null ? receiveLinkedModelsSetting.IsChecked : false;


      foreach (var @base in objects)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        {
          placeholders = null;
          break;
        }

        try
        {
          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          //skip element if is froma  linked file and setting is off
          if (!receiveLinkedModels && @base["isRevitLinkedModel"] != null && bool.Parse(@base["isRevitLinkedModel"].ToString()))
            continue;

          var convRes = converter.ConvertToNative(@base);
          if (convRes is ApplicationPlaceholderObject placeholder)
          {
            placeholders.Add(placeholder);
          }
          else if (convRes is List<ApplicationPlaceholderObject> placeholderList)
          {
            placeholders.AddRange(placeholderList);
          }
        }
        catch (Exception e)
        {
          progress.Report.LogConversionError(e);
        }
      }

      return placeholders;
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

      else
      {
        if (obj != null && !obj.GetType().IsPrimitive && !(obj is string))
          converter.Report.Log($"Skipped object of type {obj.GetType()}, not supported.");
      }

      return objects;
    }
  }
}
