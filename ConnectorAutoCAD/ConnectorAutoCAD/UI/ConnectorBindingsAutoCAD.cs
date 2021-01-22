using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Api;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.ConnectorAutoCAD.Entry;
using Speckle.Core.Transports;
using Stylet;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Colors;
using System.Collections;

namespace Speckle.ConnectorAutoCAD.UI
{
  public partial class ConnectorBindingsAutoCAD : ConnectorBindings
  {

    public static Document Doc => Application.DocumentManager.MdiActiveDocument;

    public Timer SelectionTimer;

    /// <summary>
    /// TODO: Any errors thrown should be stored here and passed to the ui state
    /// </summary>
    public List<System.Exception> Exceptions { get; set; } = new List<System.Exception>();

    public ConnectorBindingsAutoCAD() : base()
    {
      SelectionTimer = new Timer(2000) { AutoReset = true, Enabled = true };
      SelectionTimer.Elapsed += SelectionTimer_Elapsed;
      SelectionTimer.Start();
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (Doc == null)
        return;

      var selectedObjects = GetSelectedObjects();

      NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selectedObjects.Count });
      NotifyUi(new UpdateSelectionEvent() { ObjectIds = selectedObjects });
    }

    #region local streams 
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
      UserDataClass.AddStreamToSpeckleDict(state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override void RemoveStreamFromFile(string streamId)
    {
      UserDataClass.RemoveStreamFromSpeckleDict(streamId);
    }

    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      UserDataClass.UpdateStreamInSpeckleDict(state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override List<StreamState> GetStreamsInFile()
    {
      List<string> strings = UserDataClass.GetSpeckleDictStreams();
      return strings.Select(s => JsonConvert.DeserializeObject<StreamState>(s)).ToList();
    }
    #endregion

    #region boilerplate

    public override string GetActiveViewName()
    {
      return "Entire Document"; // Note: does autocad have views that filter objects?
    }

    public override List<string> GetObjectsInView()
    {
      var objs = new List<string>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        BlockTable blckTbl = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord blckTblRcrd = tr.GetObject(blckTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;
        foreach (ObjectId id in blckTblRcrd)
        {
          // Note: Checking the type from ObjectId.ObjectClass is cheaper than opening the object before checking its type?
          var dbObj = tr.GetObject(id, OpenMode.ForRead);
          if (dbObj is BlockReference)
          {
            var blckRef = (BlockReference)dbObj; // skip block references for now
          }
          else 
            objs.Add(id.ToString());
        }
        // TODO: this returns all the doc objects. Need to check for visibility later.
        tr.Commit();
      }
      return objs;
    }

    public override string GetHostAppName() => Applications.AutoCAD2021;

    public override string GetDocumentId()
    {
      string path = HostApplicationServices.Current.FindFile(Doc.Name, Doc.Database, FindFileHint.Default);
      return Speckle.Core.Models.Utilities.hashString("X" + path + Doc?.Name, Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => HostApplicationServices.Current.FindFile(Doc.Name, Doc.Database, FindFileHint.Default);

    public override string GetFileName() => Doc?.Name;

    public override List<string> GetSelectedObjects()
    {
      // TODO: this prompts user to select items: need to set to preselect?? UNSOLVED ISSUE
      List<string> objs = null;
      var entRes = Doc.Editor.GetSelection();
      if (entRes.Status == PromptStatus.OK)
        objs = entRes.Value.GetObjectIds().Select(o => o.ToString()).ToList();
      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      List<string> layers = null;
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        LayerTable lyrTbl = tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
        foreach (ObjectId objId in lyrTbl)
        {
          LayerTableRecord lyrTblRec = tr.GetObject(objId, OpenMode.ForRead) as LayerTableRecord;
          layers.Add(lyrTblRec.Name);
        }
        tr.Commit();
      }
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
      var converter = kit.LoadConverter(Applications.AutoCAD2021);
      converter.SetContextDocument(Doc);

      var myStream = await state.Client.StreamGet(state.Stream.id);
      var commit = state.Commit;

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      Exceptions.Clear();

      var commitObject = await Operations.Receive(
        commit.referencedObject,
        state.CancellationTokenSource.Token,
        new ServerTransport(state.Client.Account, state.Stream.id),
        onProgressAction: d => UpdateProgress(d, state.Progress),
        onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => state.Progress.Maximum = num),
        onErrorAction: (message, exception) => { Exceptions.Add(exception); }
        );

      if (Exceptions.Count != 0)
      {
        RaiseNotification($"Encountered some errors: {Exceptions.Last().Message}");
      }

      // var undoRecord = Doc.BeginUndoRecord($"Speckle bake operation for {myStream.name}"); history tracking, maybe use transaction wrapper?

      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        var conversionProgressDict = new ConcurrentDictionary<string, int>();
        conversionProgressDict["Conversion"] = 0;
        Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

        Action updateProgressAction = () =>
        {
          conversionProgressDict["Conversion"]++;
          UpdateProgress(conversionProgressDict, state.Progress);
        };

        var layerName = $"{myStream.name}: {state.Branch.name} @ {commit.id}";
        layerName = Regex.Replace(layerName, @"[^\u0000-\u007F]+", string.Empty); // emits emojis

        LayerTable lyrTbl = tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
        //var existingLayer = lyrTbl.FindName(layerName);

        //if (existingLayer != null)
        //{
        //  Doc.Layers.Purge(existingLayer.Id, false);
        //}
        //var layerIndex = Doc.Layers.Add(layerName, System.Drawing.Color.Blue);

        //if (layerIndex == -1)
        //{
        //  RaiseNotification($"Coould not create layer {layerName} to bake objects into.");
        //  state.Errors.Add(new Exception($"Coould not create layer {layerName} to bake objects into."));
        //  return state;
        //}
        //currentRootLayerName = layerName;
        //HandleAndConvert(commitObject, converter, Doc.Layers.FindIndex(layerIndex), state);

        //Doc.Views.Redraw();

        tr.Commit();
      }
      //Doc.EndUndoRecord(undoRecord);

      return state;
    }

    /// <summary>
    /// Used to hold in state for the handle and convert function below.
    /// </summary>
    private string currentRootLayerName;

    private void HandleAndConvert(object obj, ISpeckleConverter converter, string layer, StreamState state, Action updateProgressAction = null)
    {
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        LayerTable lyrTbl = tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
        if (!lyrTbl.Has(layer))
        {
          LayerTableRecord lyrTblRec = new LayerTableRecord();

          // Assign the layer the ACI color 1 and a name
          lyrTblRec.Color = Color.FromColorIndex(ColorMethod.ByAci, 1);
          lyrTblRec.Name = layer;

          // Upgrade the Layer table for write
          lyrTblRec.UpgradeOpen();

          // Append the new layer to the Layer table and the transaction
          lyrTbl.Add(lyrTblRec);
          tr.AddNewlyCreatedDBObject(lyrTblRec, true);

          // handle types of objs
          if (obj is Base baseItem)
          {
            if (converter.CanConvertToNative(baseItem))
            {
              var converted = converter.ConvertToNative(baseItem) as Autodesk.AutoCAD.Geometry;
              if (converted != null)
              {
                // Open the Block table for read
                BlockTable blkTbl = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;

                // Open the Block table record Model space for write
                BlockTableRecord blkTblRec = tr.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create a new object base item
                Autodesk.AutoCAD.Geometry obj = new Autodesk.AutoCAD.Geometry();
                obj.Layer = layer;

                blkTblRec.AppendEntity(obj);
                tr.AddNewlyCreatedDBObject(obj, true);
              }
              else
              {
                state.Errors.Add(new System.Exception($"Failed to convert object {baseItem.id} of type {baseItem.speckle_type}."));
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
                  layerName = prop.Remove(0, 1);
                else
                  layerName = prop;

                //var subLayer = new Layer() { ParentLayerId = layer.Id, Color = System.Drawing.Color.Gray, Name = $"{layerName}" };
                HandleAndConvert(value, converter, subLayer, state, updateProgressAction);
              }
              return;
            }
          }

          if (obj is List<object> list)
          {
            foreach (var listObj in list)
              HandleAndConvert(listObj, converter, layer, state, updateProgressAction);
            return;
          }

          if (obj is IDictionary dict)
          {
            foreach (DictionaryEntry kvp in dict)
              HandleAndConvert(kvp.Value, converter, layer, state, updateProgressAction);
            return;
          }
        }
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

      var units = Units.GetUnitsFromString();
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

        // this is where the geometry gets converted
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

        var layerName = Doc.Layers[obj.Attributes.LayerIndex].FullPath;

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
      catch (System.Exception e)
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
