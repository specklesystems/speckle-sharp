using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections;
using System.Drawing;
using Newtonsoft.Json;

using AcadApp = Autodesk.AutoCAD.ApplicationServices;
using AcadDb = Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using CivilApp = Autodesk.Civil.ApplicationServices;
using CivilDb = Autodesk.Civil.DatabaseServices;

using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Api;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.ConnectorAutoCAD;
using Speckle.Core.Transports;
using Stylet;

namespace Speckle.ConnectorAutoCAD.UI
{
  public partial class ConnectorBindingsAutoCAD : ConnectorBindings
  {

    public AcadApp.Document Doc => AcadApp.Application.DocumentManager.MdiActiveDocument;
    public CivilApp.CivilDocument DocCivil => CivilApp.CivilApplication.ActiveDocument;

    public Timer SelectionTimer;

    /// <summary>
    /// TODO: Any errors thrown should be stored here and passed to the ui state
    /// </summary>
    public List<System.Exception> Exceptions { get; set; } = new List<System.Exception>();

    public ConnectorBindingsAutoCAD() : base()
    {
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
      SpeckleStream.AddSpeckleStream(state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override void RemoveStreamFromFile(string streamId)
    {
      SpeckleStream.RemoveSpeckleStream(streamId);
    }

    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      SpeckleStream.UpdateSpeckleStream(state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override List<StreamState> GetStreamsInFile()
    {
      List<string> strings = SpeckleStream.GetSpeckleStreams();
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
      using (AcadDb.Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        AcadDb.BlockTable blckTbl = tr.GetObject(Doc.Database.BlockTableId, AcadDb.OpenMode.ForRead) as AcadDb.BlockTable;
        AcadDb.BlockTableRecord blckTblRcrd = tr.GetObject(blckTbl[AcadDb.BlockTableRecord.ModelSpace], AcadDb.OpenMode.ForRead) as AcadDb.BlockTableRecord;
        foreach (AcadDb.ObjectId id in blckTblRcrd)
        {
          var dbObj = tr.GetObject(id, AcadDb.OpenMode.ForRead);
          if (dbObj is AcadDb.BlockReference)
          {
            var blckRef = (AcadDb.BlockReference)dbObj; // skip block references for now
          }
          else 
            objs.Add(dbObj.Handle.ToString());
        }
        // TODO: this returns all the doc objects. Need to check for visibility later.
        tr.Commit();
      }
      return objs;
    }

    public override string GetHostAppName() => Applications.AutoCAD2021;

    public override string GetDocumentId()
    {
      string path = AcadDb.HostApplicationServices.Current.FindFile(Doc.Name, Doc.Database, AcadDb.FindFileHint.Default);
      return Speckle.Core.Models.Utilities.hashString("X" + path + Doc?.Name, Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => AcadDb.HostApplicationServices.Current.FindFile(Doc.Name, Doc.Database, AcadDb.FindFileHint.Default);

    public override string GetFileName() => Doc?.Name;

    public override List<string> GetSelectedObjects()
    {
      // TODO: use enums or props to capture command names, none of this string chasing bs
      Doc.SendStringToExecute("SpeckleSelection ", false, false, true);
      var objs = UserData.GetSpeckleSelection;
      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      List<string> layers = new List<string>();
      using (AcadDb.Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        AcadDb.LayerTable lyrTbl = tr.GetObject(Doc.Database.LayerTableId, AcadDb.OpenMode.ForRead) as AcadDb.LayerTable;
        foreach (AcadDb.ObjectId objId in lyrTbl)
        {
          AcadDb.LayerTableRecord lyrTblRec = tr.GetObject(objId, AcadDb.OpenMode.ForRead) as AcadDb.LayerTableRecord;
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
      var converter = kit.LoadConverter(ConnectorAutoCADUtils.AutoCADAppName);
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

      // add rollback here?

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);
      };

      // create a layer prefix hash: this is to prevent geometry from being imported into original layers (too confusing)
      // since autocad doesn't have nested layers, use the standard import syntax of "layer$sublayer" when importing from apps that have nested layers
      var layerPrefix = $"{myStream.name}[{state.Branch.name}@{commit.id}]";
      layerPrefix = Regex.Replace(layerPrefix, @"[^\u0000-\u007F]+", string.Empty); // emits emojis

      // see if there is already an existing layer with this prefix - if there is then this commit has already been recieved?
      DeleteLayersWithPrefix(layerPrefix);

      // try and import geo
      HandleAndConvert(commitObject, converter, layerPrefix, state);

      return state;
    }
   

    private void HandleAndConvert(object obj, ISpeckleConverter converter, string layerPrefix, StreamState state, Action updateProgressAction = null)
    {
      if (obj is Base baseItem)
      {
        if (converter.CanConvertToNative(baseItem))
        {
          // create the ac layer if it doesn't already exist
          AcadDb.LayerTableRecord objLayer = GetOrMakeLayer(layerPrefix);
          if (objLayer == null)
          {
            RaiseNotification($"could not create layer {layerPrefix} to bake objects into.");
            state.Errors.Add(new System.Exception($"could not create layer {layerPrefix} to bake objects into."));
            updateProgressAction?.Invoke();
            return;
          }

          // convert geo to native
          var converted = converter.ConvertToNative(baseItem) as AcadDb.Entity;

          // add geo to doc
          if (converted != null)
          {
            converted.Append();
            //converted.Dispose();
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
            string objLayerName;
            if (prop.StartsWith("@"))
              objLayerName = prop.Remove(0, 1);
            else
              objLayerName = prop;

            // create the ac layer if it doesn't already exist
            string acLayerName = $"{layerPrefix}${objLayerName}";
            AcadDb.LayerTableRecord objLayer = GetOrMakeLayer(acLayerName);
            if (objLayer == null)
            {
              RaiseNotification($"could not create layer {acLayerName} to bake objects into.");
              state.Errors.Add(new System.Exception($"could not create layer {acLayerName} to bake objects into."));
              updateProgressAction?.Invoke();
              return;
            }
            HandleAndConvert(value, converter, acLayerName, state, updateProgressAction);
          }
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
          HandleAndConvert(listObj, converter, layerPrefix, state, updateProgressAction);
        return;
      }

      if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
          HandleAndConvert(kvp.Value, converter, layerPrefix, state, updateProgressAction);
        return;
      }
      
    }

    private void DeleteLayersWithPrefix(string prefix)
    {
      using (AcadApp.DocumentLock l = Doc.LockDocument())
      {
        using (AcadDb.Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          // Open the Layer table for read
          AcadDb.LayerTable lyrTbl;
          lyrTbl = tr.GetObject(Doc.Database.LayerTableId, AcadDb.OpenMode.ForRead) as AcadDb.LayerTable;
          foreach (AcadDb.ObjectId layerId in lyrTbl)
          {
            AcadDb.LayerTableRecord layer = (AcadDb.LayerTableRecord)tr.GetObject(layerId, AcadDb.OpenMode.ForRead);
            string layerName = layer.Name;
            if (layerName.StartsWith(prefix))
            {
              layer.UpgradeOpen();
              if (Doc.Database.Clayer == layerId)
              {
                var defaultLayerID = lyrTbl["0"];
                Doc.Database.Clayer = defaultLayerID;
              }
              layer.IsLocked = false;

              // delete all objects on this layer .. todo: this is inefficient! find better way to deleting obs instea dof looping through each one
              var blockTable = (AcadDb.BlockTable)tr.GetObject(Doc.Database.BlockTableId, AcadDb.OpenMode.ForRead);
              foreach (var btrId in blockTable)
              {
                var block = (AcadDb.BlockTableRecord)tr.GetObject(btrId, AcadDb.OpenMode.ForRead);
                foreach (var entId in block)
                {
                  var ent = (AcadDb.Entity)tr.GetObject(entId, AcadDb.OpenMode.ForRead);
                  if (ent.Layer == layerName)
                  {
                    ent.UpgradeOpen();
                    ent.Erase();
                  }
                }
              }
              layer.Erase();
            }
          }
          tr.Commit();
        }
      }
    }

    private AcadDb.LayerTableRecord GetOrMakeLayer(string layerName)
    {
      AcadDb.LayerTableRecord _layer = null;
      using (AcadApp.DocumentLock l = Doc.LockDocument())
      {
        using (AcadDb.Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          AcadDb.LayerTable lyrTbl = tr.GetObject(Doc.Database.LayerTableId, AcadDb.OpenMode.ForRead) as AcadDb.LayerTable;
          if (lyrTbl.Has(layerName))
          {
            _layer = (AcadDb.LayerTableRecord)tr.GetObject(lyrTbl[layerName], AcadDb.OpenMode.ForRead);
          }
          else
          {
            lyrTbl.UpgradeOpen();

            // make a new layer
            AcadDb.LayerTableRecord layer = new AcadDb.LayerTableRecord();

            // Assign the layer properties
            layer.Color = Autodesk.AutoCAD.Colors.Color.FromColor(System.Drawing.Color.Blue);
            layer.Name = layerName;

            // Append the new layer to the Layer table and the transaction
            lyrTbl.Add(layer);
            tr.AddNewlyCreatedDBObject(layer, true);
            _layer = layer;
          }
          tr.Commit();
        }
      }
      return _layer;
    }

    #endregion

    #region sending

    public override async Task<StreamState> SendStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(ConnectorAutoCADUtils.AutoCADAppName);
      converter.SetContextDocument(Doc);

      var streamId = state.Stream.id;
      var client = state.Client;

      if (state.Filter != null)
      {
        state.SelectedObjectIds = GetObjectsFromFilter(state.Filter);
      }
      if (state.SelectedObjectIds.Count == 0)
      {
        RaiseNotification("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
        return state;
      }

      var commitObj = new Base();

      var units = Units.GetUnitsFromString(Doc.Database.Insunits.ToString());
      commitObj["units"] = units;

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());
      int convertedCount = 0;

      foreach (var autocadObjectHandle in state.SelectedObjectIds)
      {
        if (state.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return null;
        }

        // get the db object from id NOTE: This is a db object, not a geometry object!! Need to pass the geo object to converter
        AcadDb.Handle hn = new AcadDb.Handle(Convert.ToInt64(autocadObjectHandle, 16));
        AcadDb.DBObject obj = hn.GetObject(out string type, out string layer);
        if (obj == null)
        {
          state.Errors.Add(new System.Exception($"Failed to find local object ${autocadObjectHandle}."));
          continue;
        }

        // convert geo to speckle base
        Base converted = converter.ConvertToSpeckle(obj as AcadDb.DBObject);

        if (converted == null)
        {
          state.Errors.Add(new System.Exception($"Failed to find convert object ${autocadObjectHandle} of type ${type}."));
          continue;
        }

        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);

        converted.applicationId = autocadObjectHandle;

        /* TODO: adding the extension dictionary / xdata per object 
        foreach (var key in obj.ExtensionDictionary)
        {
          converted[key] = obj.ExtensionDictionary.GetUserString(key);
        }
        */

        if (commitObj[$"@{layer}"] == null)
        {
          commitObj[$"@{layer}"] = new List<Base>();
        }

        ((List<Base>)commitObj[$"@{layer}"]).Add(converted);

        convertedCount++;
      }

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      Execute.PostToUIThread(() => state.Progress.Maximum = convertedCount);

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var commitObjId = await Operations.Send(
        commitObj,
        state.CancellationTokenSource.Token,
        transports,
        onProgressAction: dict => UpdateProgress(dict, state.Progress),
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
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {convertedCount} elements from AutoCAD.",
        sourceApplication = ConnectorAutoCADUtils.AutoCADAppName
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);

        await state.RefreshStream();
        state.PreviousCommitId = commitId;

        PersistAndUpdateStreamInFile(state);
        RaiseNotification($"{convertedCount} objects sent to {state.Stream.name}.");
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
            AcadDb.TypedValue[] layerType = new AcadDb.TypedValue[1] { new AcadDb.TypedValue((int)AcadDb.DxfCode.LayerName, layerName)};
            PromptSelectionResult prompt = Doc.Editor.SelectAll(new SelectionFilter(layerType));
            if (prompt.Status == PromptStatus.OK)
              objs.AddRange(prompt.Value.GetHandles());
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
