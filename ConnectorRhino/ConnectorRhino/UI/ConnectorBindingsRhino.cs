using Speckle.Newtonsoft.Json;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Stylet;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace SpeckleRhino
{
  public partial class ConnectorBindingsRhino : ConnectorBindings
  {

    public RhinoDoc Doc { get => RhinoDoc.ActiveDoc; }

    public Timer SelectionTimer;

    /// <summary>
    /// TODO: Any errors thrown should be stored here and passed to the ui state (somehow).
    /// </summary>
    public List<Exception> Exceptions { get; set; } = new List<Exception>();

    public ConnectorBindingsRhino()
    {
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;

      SelectionTimer = new Timer(2000) { AutoReset = true, Enabled = true };
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      SelectionTimer.Start();
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (Doc == null)
      {
        return;
      }

      var selection = GetSelectedObjects();

      NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selection.Count });
      NotifyUi(new UpdateSelectionEvent() { ObjectIds = selection });
    }

    private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge)
      {
        return; // prevents triggering this on copy pastes, imports, etc.
      }

      if (e.Document == null)
      {
        return;
      }

      GetFileContextAndNotifyUI();
    }

    #region Local streams I/O with local file

    public void GetFileContextAndNotifyUI()
    {
      var streamStates = GetStreamsInFile();

      var appEvent = new ApplicationEvent()
      {
        Type = ApplicationEvent.EventType.DocumentOpened,
        DynamicInfo = streamStates
      };

      NotifyUi(appEvent);
    }

    public override void AddNewStream(StreamState state)
    {
      Doc.Strings.SetString("speckle", state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override void RemoveStreamFromFile(string streamId)
    {
      Doc.Strings.Delete("speckle", streamId);
    }

    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      Doc.Strings.SetString("speckle", state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var strings = Doc?.Strings.GetEntryNames("speckle");
      if (strings == null)
      {
        return new List<StreamState>();
      }

      return strings.Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue("speckle", s))).ToList();
    }

    #endregion

    #region boilerplate

    public override string GetActiveViewName()
    {
      return "Entire Document"; // Note: rhino does not have views that filter objects.
    }

    public override List<string> GetObjectsInView()
    {
      var objs = Doc.Objects.Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList(); // Note: this returns all the doc objects.
      return objs;
    }

    public override string GetHostAppName() => Applications.Rhino;

    public override string GetDocumentId()
    {
      return Speckle.Core.Models.Utilities.hashString("X" + Doc?.Path + Doc?.Name, Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => Doc?.Path;

    public override string GetFileName() => Doc?.Name;

    public override List<string> GetSelectedObjects()
    {
      var objs = Doc?.Objects.GetSelectedObjects(true, false).Select(obj => obj.Id.ToString()).ToList();
      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var layers = Doc.Layers.ToList().Select(layer => layer.Name).ToList();

      return new List<ISelectionFilter>()
      {
         new ListSelectionFilter { Name = "Layers", Icon = "Filter", Description = "Selects objects based on their layers.", Values = layers }
      };
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    private void UpdateProgress(ConcurrentDictionary<string, int> dict, ProgressReport progress)
    {
      if (progress == null)
      {
        return;
      }

      Execute.PostToUIThread(() =>
      {
        progress.ProgressDict = dict;
        progress.Value = dict.Values.Last();
      });
    }

    #endregion

    #region receiving 

    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Rhino);
      converter.SetContextDocument(Doc);

      var myStream = await state.Client.StreamGet(state.Stream.id);

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var transport = new ServerTransport(state.Client.Account, state.Stream.id);

      Exceptions.Clear();

      string referencedObject = state.Commit.referencedObject;

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
        onProgressAction: d => UpdateProgress(d, state.Progress),
        onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => state.Progress.Maximum = num),
        onErrorAction: (message, exception) => { Exceptions.Add(exception); }
        );

      if (Exceptions.Count != 0)
      {
        RaiseNotification($"Encountered some errors: {Exceptions.Last().Message}");
      }

      var undoRecord = Doc.BeginUndoRecord($"Speckle bake operation for {myStream.name}");

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);
      };

      var layerName = $"{myStream.name}: {state.Branch.name} @ {commit.id}";
      layerName = Regex.Replace(layerName, @"[^\u0000-\u007F]+", string.Empty); // Rhino doesn't like emojis in layer names :( 

      var existingLayer = Doc.Layers.FindName(layerName);

      if (existingLayer != null)
      {
        Doc.Layers.Purge(existingLayer.Id, false);
      }
      var layerIndex = Doc.Layers.Add(layerName, System.Drawing.Color.Blue);

      if (layerIndex == -1)
      {
        RaiseNotification($"Coould not create layer {layerName} to bake objects into.");
        state.Errors.Add(new Exception($"Coould not create layer {layerName} to bake objects into."));
        return state;
      }
      currentRootLayerName = layerName;
      HandleAndConvert(commitObject, converter, Doc.Layers.FindIndex(layerIndex), state);

      Doc.Views.Redraw();

      Doc.EndUndoRecord(undoRecord);

      return state;
    }

    /// <summary>
    /// Used to hold in state for the handle and convert function below.
    /// </summary>
    private string currentRootLayerName;

    private void HandleAndConvert(object obj, ISpeckleConverter converter, Layer layer, StreamState state, Action updateProgressAction = null)
    {
      Layer myLayer = null;

      // The rhino layer api is a bit sucky, hence the result below. It probably can be cleaned up and optimised.
      if (!layer.HasIndex || layer.Index == -1)
      {
        // Try and recreate layer structure if coming from Rhino.
        if (layer.Name.Contains("::") || layer.FullPath.Contains("::"))
        {
          var layers = layer.Name.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
          var ancestors = new List<Layer>();
          var currentPath = currentRootLayerName;
          foreach (var linkName in layers)
          {
            currentPath += $"::{linkName}";
            var existingIndex = Doc.Layers.FindByFullPath(currentPath, -1);
            if (existingIndex != -1)
            {
              ancestors.Add(Doc.Layers[existingIndex]);
            }
            else
            {
              var newLayer = new Layer() { Color = System.Drawing.Color.AliceBlue, Name = linkName };
              if (ancestors.Count != 0)
              {
                newLayer.ParentLayerId = ancestors.Last().Id;
              }
              else
              {
                newLayer.ParentLayerId = layer.ParentLayerId;
              }
              var newIndex = Doc.Layers.Add(newLayer);
              ancestors.Add(Doc.Layers[newIndex]);
            }

            layer = ancestors.Last();
          }
        }
        else
        {
          var index = Doc.Layers.Add(layer);
          if (index == -1) // it means it exists already, and we're returning to a previously created higher level layer.
          {
            var fullPath = "";
            if (layer.ParentLayerId != null)
            {
              var parent = Doc.Layers.FindId(layer.ParentLayerId);
              fullPath += parent.FullPath + "::" + layer.Name;
            }
            var existingLayerIndex = Doc.Layers.FindByFullPath(fullPath, true);
            layer.Index = Doc.Layers.FindIndex(existingLayerIndex).Index;
          }
          else
          {
            layer.Index = index;
          }
        }
      }

      layer = Doc.Layers.FindIndex(layer.Index);

      if (obj is Base baseItem)
      {
        if (converter.CanConvertToNative(baseItem))
        {
          var converted = converter.ConvertToNative(baseItem) as Rhino.Geometry.GeometryBase;
          if (converted != null)
          {
            Doc.Objects.Add(converted, new ObjectAttributes { LayerIndex = layer.Index });
          }
          else
          {
            state.Errors.Add(new Exception($"Failed to convert object {baseItem.id} of type {baseItem.speckle_type}."));
          }
          updateProgressAction?.Invoke();
          return;
        }
        else
        {

          foreach (var prop in baseItem.GetDynamicMembers())
          {
            var value = baseItem[prop];
            string layerName;
            if (prop.StartsWith("@"))
            {
              layerName = prop.Remove(0, 1);
            }
            else
            {
              layerName = prop;
            }

            var subLayer = new Layer() { ParentLayerId = layer.Id, Color = System.Drawing.Color.Gray, Name = $"{layerName}" };
            HandleAndConvert(value, converter, subLayer, state, updateProgressAction);
          }

          return;
        }
      }

      if (obj is List<object> list)
      {

        foreach (var listObj in list)
        {
          HandleAndConvert(listObj, converter, layer, state, updateProgressAction);
        }
        return;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          HandleAndConvert(kvp.Value, converter, layer, state, updateProgressAction);
        }
        return;
      }
    }

    #endregion

    #region sending

    public override async Task<StreamState> SendStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Rhino);
      converter.SetContextDocument(Doc);
      Exceptions.Clear();

      var commitObj = new Base();

      var units = Units.GetUnitsFromString(Doc.GetUnitSystemName(true, false, false, false));
      commitObj["units"] = units;

      int objCount = 0;

      // TODO: check for filters and trawl the doc.
      if (state.Filter != null)
      {
        state.SelectedObjectIds = GetObjectsFromFilter(state.Filter);
      }

      if (state.SelectedObjectIds.Count == 0)
      {
        RaiseNotification("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
        return state;
      }

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      foreach (var applicationId in state.SelectedObjectIds)
      {
        if (state.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return null;
        }

        var obj = Doc.Objects.FindId(new Guid(applicationId));
        if (obj == null)
        {
          state.Errors.Add(new Exception($"Failed to find local object ${applicationId}."));
          continue;
        }

        // this is where the rhino geometry gets converted
        Base converted = converter.ConvertToSpeckle(obj);
        if (converted == null)
        {
          state.Errors.Add(new Exception($"Failed to find convert object ${applicationId} of type ${obj.Geometry.ObjectType.ToString()}."));
          continue;
        }

        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);

        // TODO: potentially get more info from the object: materials and other rhino specific stuff?
        converted.applicationId = applicationId;

        foreach (var key in obj.Attributes.GetUserStrings().AllKeys)
        {
          // TODO: check if this is a SchemaBuilder key and maybe omit?
          converted[key] = obj.Attributes.GetUserString(key);
        }

        var layerName = Doc.Layers[obj.Attributes.LayerIndex].FullPath; // sep is ::

        if (commitObj[$"@{layerName}"] == null)
        {
          commitObj[$"@{layerName}"] = new List<Base>();
        }

        ((List<Base>)commitObj[$"@{layerName}"]).Add(converted);

        objCount++;
      }

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      Execute.PostToUIThread(() => state.Progress.Maximum = objCount);

      var streamId = state.Stream.id;
      var client = state.Client;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var commitObjId = await Operations.Send(
        commitObj,
        state.CancellationTokenSource.Token,
        transports,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
        /* TODO: a wee bit nicer handling here; plus request cancellation! */
        onErrorAction: (err, exception) => { Exceptions.Add(exception); }
        );

      if (Exceptions.Count != 0)
      {
        RaiseNotification($"Failed to send: \n {Exceptions.Last().Message}");
        return null;
      }

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = commitObjId,
        branchName = state.Branch.name,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {objCount} elements from Rhino.",
        sourceApplication = Applications.Rhino
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);

        await state.RefreshStream();
        state.PreviousCommitId = commitId;

        PersistAndUpdateStreamInFile(state);
        RaiseNotification($"{objCount} objects sent to {state.Stream.name}.");
      }
      catch (Exception e)
      {
        Globals.Notify($"Failed to create commit.\n{e.Message}");
        state.Errors.Add(e);
      }

      return state;
    }

    private List<string> GetObjectsFromFilter(ISelectionFilter filter)
    {
      switch (filter)
      {
        case ListSelectionFilter f:
          List<string> objs = new List<string>();
          foreach (var layerName in f.Selection)
          {
            var docObjs = Doc.Objects.FindByLayer(layerName).Select(o => o.Id.ToString());
            objs.AddRange(docObjs);
          }
          return objs;
        default:
          RaiseNotification("Filter type is not supported in this app. Why did the developer implement it in the first place?");
          return new List<string>();
      }
    }

    #endregion

  }
}
