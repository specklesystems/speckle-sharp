using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ConnectorRevit;
using ConnectorRevit.Revit;
using Speckle.ConnectorRevit.Entry;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.DesktopUI.Utils;
using Stylet;
using RevitElement = Autodesk.Revit.DB.Element;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit
  {

    /// <summary>
    /// Receives a stream and bakes into the existing revit file.
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      ConversionErrors.Clear();
      OperationErrors.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorRevitUtils.RevitAppName);
      converter.SetContextDocument(CurrentDoc.Document);
      var previouslyReceiveObjects = state.ReceivedObjects;

      var transport = new ServerTransport(state.Client.Account, state.Stream.id);

      string referencedObject = state.Commit.referencedObject;

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      if (state.Commit.id == "latest")
      {
        var res = await state.Client.BranchGet(state.CancellationTokenSource.Token, state.Stream.id, state.Branch.name, 1);
        referencedObject = res.commits.items.FirstOrDefault().referencedObject;
      }

      var commit = state.Commit;

      var commitObject = await Operations.Receive(
          referencedObject,
          state.CancellationTokenSource.Token,
          transport,
          onProgressAction: dict => UpdateProgress(dict, state.Progress),
          onErrorAction: (s, e) =>
          {
            OperationErrors.Add(e);
            state.Errors.Add(e);
            state.CancellationTokenSource.Cancel();
          },
          onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count),
          disposeTransports: true
          );

      if (OperationErrors.Count != 0)
      {
        Globals.Notify("Failed to get commit.");
        return state;
      }

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      // Bake the new ones.
      Queue.Add(() =>
      {
        using (var t = new Transaction(CurrentDoc.Document, $"Baking stream {state.Stream.name}"))
        {
          var failOpts = t.GetFailureHandlingOptions();
          failOpts.SetFailuresPreprocessor(new ErrorEater(converter));
          failOpts.SetClearAfterRollback(true);
          t.SetFailureHandlingOptions(failOpts);

          t.Start();
          var flattenedObjects = FlattenCommitObject(commitObject, converter);
          // needs to be set for editing to work 
          converter.SetPreviousContextObjects(previouslyReceiveObjects);
          // needs to be set for openings in floors and roofs to work
          converter.SetContextObjects(flattenedObjects.Select(x => new ApplicationObject(x.id, ConnectorRevitUtils.SimplifySpeckleType(x.speckle_type)) { applicationId = x.applicationId }).ToList());
          var newPlaceholderObjects = ConvertReceivedObjects(flattenedObjects, converter, state);
          // receive was cancelled by user
          if (newPlaceholderObjects == null)
          {
            converter.Report.LogConversionError(new Exception("fatal error: receive cancelled by user"));
            t.RollBack();
            return;
          }

          DeleteObjects(previouslyReceiveObjects, newPlaceholderObjects);

          state.ReceivedObjects = newPlaceholderObjects;

          t.Commit();

          state.Errors.AddRange(converter.Report.ConversionErrors);
        }

      });

      Executor.Raise();

      while (Queue.Count > 0)
      {
        //wait to let queue finish
      }

      if (converter.Report.ConversionErrorsString.Contains("fatal error"))
      {
        // the commit is being rolled back
        return null;
      }

      try
      {
        await state.RefreshStream();

        WriteStateToFile();
      }
      catch (Exception e)
      {
        WriteStateToFile();
        state.Errors.Add(e);
        Globals.Notify($"Receiving done, but failed to update stream from server.\n{e.Message}");
      }

      return state;
    }

    //delete previously sent object that are no more in this stream
    private void DeleteObjects(List<ApplicationObject> previouslyReceiveObjects, List<ApplicationObject> newPlaceholderObjects)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        var element = CurrentDoc.Document.GetElement(obj.CreatedIds.FirstOrDefault());
        if (element != null)
          CurrentDoc.Document.Delete(element.Id);
      }
    }

    private List<ApplicationObject> ConvertReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state)
    {
      var placeholders = new List<ApplicationObject>();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 1;

      foreach (var @base in objects)
      {
        if (state.CancellationTokenSource.Token.IsCancellationRequested)
        {
          placeholders = null;
          break;
        }

        try
        {
          conversionProgressDict["Conversion"]++;
          // wrapped in a dispatcher not to block the ui
          SpeckleRevitCommand.Bootstrapper.RootWindow.Dispatcher.Invoke(() =>
          {
            UpdateProgress(conversionProgressDict, state.Progress);
          }, System.Windows.Threading.DispatcherPriority.Background);

          var convRes = converter.ConvertToNative(@base);
          if (convRes is ApplicationObject placeholder)
            placeholders.Add(placeholder);
          else if (convRes is List<ApplicationObject> placeholderList)
            placeholders.AddRange(placeholderList);
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
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

      return objects;
    }
  }
}
