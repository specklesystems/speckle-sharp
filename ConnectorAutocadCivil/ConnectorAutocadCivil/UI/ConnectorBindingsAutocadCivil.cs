using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Speckle.ConnectorAutocadCivil.Entry;
using Speckle.ConnectorAutocadCivil.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.ConnectorAutocadCivil.UI
{
  public partial class ConnectorBindingsAutocad : ConnectorBindings
  {
    public static Document Doc => Application.DocumentManager.MdiActiveDocument;

    public List<string> GetLayers()
    {
      var layers = new List<string>();
      foreach (var docLayer in Application.UIBindings.Collections.Layers)
      {
        var name = docLayer.GetProperties().Find("Name", true).GetValue(docLayer);
        layers.Add(name as string);
      }
      return layers;
    }

    // AutoCAD API should only be called on the main thread.
    // Not doing so results in botched conversions for any that require adding objects to Document model space before modifying (eg adding vertices and faces for meshes)
    // There's no easy way to access main thread from document object, therefore we are creating a control during Connector Bindings constructor (since it's called on main thread) that allows for invoking worker threads on the main thread
    public System.Windows.Forms.Control Control;
    public ConnectorBindingsAutocad() : base()
    {
      Control = new System.Windows.Forms.Control();
      Control.CreateControl();
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Create };
    }

    #region local streams 
    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      SpeckleStreamManager.WriteStreamStateList(Doc, streams);
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var streams = new List<StreamState>();
      if (Doc != null)
        streams = SpeckleStreamManager.ReadState(Doc);
      return streams;
    }
    #endregion

    #region boilerplate
    public override string GetHostAppNameVersion() => Utils.VersionedAppName.Replace("AutoCAD", "AutoCAD ").Replace("Civil", "Civil 3D  "); //hack for ADSK store;

    public override string GetHostAppName() => Utils.Slug;

    private string GetDocPath(Document doc) => HostApplicationServices.Current.FindFile(doc?.Name, doc?.Database, FindFileHint.Default);

    public override string GetDocumentId()
    {
      string path = GetDocPath(Doc);
      var hash = Core.Models.Utilities.hashString(path + Doc?.Name, Core.Models.Utilities.HashingFuctions.MD5);
      return hash;
    }

    public override string GetDocumentLocation() => GetDocPath(Doc);

    public override string GetFileName() => (Doc != null) ? System.IO.Path.GetFileName(Doc.Name) : string.Empty;

    public override string GetActiveViewName() => "Entire Document";

    public override List<string> GetObjectsInView() // this returns all visible doc objects.
    {
      var objs = new List<string>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        BlockTableRecord modelSpace = Doc.Database.GetModelSpace();
        foreach (ObjectId id in modelSpace)
        {
          var dbObj = tr.GetObject(id, OpenMode.ForRead);
          if (dbObj.Visible())
            objs.Add(dbObj.Handle.ToString());
        }
        tr.Commit();
      }
      return objs;
    }

    public override List<string> GetSelectedObjects()
    {
      var objs = new List<string>();
      if (Doc != null)
      {
        PromptSelectionResult selection = Doc.Editor.SelectImplied();
        if (selection.Status == PromptStatus.OK)
          objs = selection.Value.GetHandles();
      }
      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      return new List<ISelectionFilter>()
      {
        new ManualSelectionFilter(),
        new ListSelectionFilter {Slug="layer",  Name = "Layers", Icon = "LayersTriple", Description = "Selects objects based on their layers.", Values = GetLayers() },
        new AllSelectionFilter {Slug="all",  Name = "Everything", Icon = "CubeScan", Description = "Selects all document objects." }
      };
    }

    public override List<ISetting> GetSettings()
    {
      return new List<ISetting>();
    }

    //TODO
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region receiving 
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Utils.VersionedAppName);
      if (converter == null)
        throw new Exception("Could not find any Kit!");
      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var stream = await state.Client.StreamGet(state.StreamId);

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      if (Doc == null)
      {
        progress.Report.LogOperationError(new Exception($"No Document is open."));
        progress.CancellationTokenSource.Cancel();
      }

      //if "latest", always make sure we get the latest commit when the user clicks "receive"
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
      Base commitObject = null;
      try
      {
        commitObject = await Operations.Receive(
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

        await state.Client.CommitReceived(new CommitReceivedInput
        {
          streamId = stream?.id,
          commitId = commit?.id,
          message = commit?.message,
          sourceApplication = Utils.VersionedAppName
        });
      }
      catch (Exception e)
      {
        progress.Report.OperationErrors.Add(new Exception($"Could not receive or deserialize commit: {e.Message}"));
      }
      if (progress.Report.OperationErrorsCount != 0 || commitObject == null)
        return state;

      // invoke conversions on the main thread via control
      if (Control.InvokeRequired)
        Control.Invoke(new ReceivingDelegate(ConvertReceiveCommit), new object[] { commitObject, converter, state, progress, stream, commit.id });
      else
        ConvertReceiveCommit(commitObject, converter, state, progress, stream, commit.id);

      return state;
    }

    delegate void ReceivingDelegate(Base commitObject, ISpeckleConverter converter, StreamState state, ProgressViewModel progress, Stream stream, string id);
    private void ConvertReceiveCommit(Base commitObject, ISpeckleConverter converter, StreamState state, ProgressViewModel progress, Stream stream, string id)
    {
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          // set the context doc for conversion - this is set inside the transaction loop because the converter retrieves this transaction for all db editing when the context doc is set!
          converter.SetContextDocument(Doc);

          // keep track of conversion progress here
          var conversionProgressDict = new ConcurrentDictionary<string, int>();
          conversionProgressDict["Conversion"] = 1;

          // keep track of any layer name changes for notification here
          bool changedLayerNames = false;

          // create a commit prefix: used for layers and block definition names
          var commitPrefix = Formatting.CommitInfo(stream.name, state.BranchName, id);

          // give converter a way to access the commit info
          if (Doc.UserData.ContainsKey("commit"))
            Doc.UserData["commit"] = commitPrefix;
          else
            Doc.UserData.Add("commit", commitPrefix);

          // delete existing commit layers
          try
          {
            DeleteBlocksWithPrefix(commitPrefix, tr);
            DeleteLayersWithPrefix(commitPrefix, tr);
          }
          catch
          {
            converter.Report.LogOperationError(new Exception($"Failed to remove existing layers or blocks starting with {commitPrefix} before importing new geometry."));
          }

          // flatten the commit object to retrieve children objs
          int count = 0;
          var commitObjs = FlattenCommitObject(commitObject, converter, commitPrefix, state, ref count);

          // open model space block table record for write
          BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);

          // More efficient this way than doing this per object
          var lineTypeDictionary = new Dictionary<string, ObjectId>();
          var lineTypeTable = (LinetypeTable)tr.GetObject(Doc.Database.LinetypeTableId, OpenMode.ForRead);
          foreach (ObjectId lineTypeId in lineTypeTable)
          {
            var linetype = (LinetypeTableRecord)tr.GetObject(lineTypeId, OpenMode.ForRead);
            lineTypeDictionary.Add(linetype.Name, lineTypeId);
          }

          foreach (var commitObj in commitObjs)
          {
            // create the object's bake layer if it doesn't already exist
            (Base obj, string layerName) = commitObj;

            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);

            object converted = null;
            try
            {
              converted = converter.ConvertToNative(obj);
            }
            catch (Exception e)
            {
              progress.Report.LogConversionError(new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}: {e.Message}"));
              continue;
            }
            var convertedEntity = converted as Entity;

            if (convertedEntity != null)
            {
              if (GetOrMakeLayer(layerName, tr, out string cleanName))
              {
                // record if layer name has been modified
                if (!cleanName.Equals(layerName))
                  changedLayerNames = true;

                var res = convertedEntity.Append(cleanName);
                if (res.IsValid)
                {
                  // handle display - fallback to rendermaterial if no displaystyle exists
                  Base display = obj[@"displayStyle"] as Base;
                  if (display == null) display = obj[@"renderMaterial"] as Base;
                  if (display != null) Utils.SetStyle(display, convertedEntity, lineTypeDictionary);

                  tr.TransactionManager.QueueForGraphicsFlush();
                }
                else
                {
                  progress.Report.LogConversionError(new Exception($"Failed to add converted object {obj.id} of type {obj.speckle_type} to the document."));
                }

              }
              else
                progress.Report.LogOperationError(new Exception($"Failed to create layer {layerName} to bake objects into."));
            }
            else if (converted == null)
            {
              progress.Report.LogConversionError(new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}."));
            }
          }
          progress.Report.Merge(converter.Report);

          if (changedLayerNames)
            progress.Report.Log($"Layer names were modified: one or more layers contained invalid characters {Utils.invalidChars}");

          // remove commit info from doc userdata
          Doc.UserData.Remove("commit");

          tr.Commit();
        }
      }
    }
    // Recurses through the commit object and flattens it. Returns list of Base objects with their bake layers
    private List<Tuple<Base, string>> FlattenCommitObject(object obj, ISpeckleConverter converter, string layer, StreamState state, ref int count, bool foundConvertibleMember = false)
    {
      var objects = new List<Tuple<Base, string>>();

      if (obj is Base @base)
      {
        if (converter.CanConvertToNative(@base))
        {
          objects.Add(new Tuple<Base, string>(@base, layer));
          return objects;
        }
        else
        {
          List<string> props = @base.GetDynamicMembers().ToList();
          if (@base.GetMembers().ContainsKey("displayValue"))
            props.Add("displayValue");
          else if (@base.GetMembers().ContainsKey("displayMesh")) // add display mesh to member list if it exists. this will be deprecated soon
            props.Add("displayMesh");
          if (@base.GetMembers().ContainsKey("elements")) // this is for builtelements like roofs, walls, and floors.
            props.Add("elements");
          int totalMembers = props.Count;

          foreach (var prop in props)
          {
            count++;

            // get bake layer name
            string objLayerName = prop.StartsWith("@") ? prop.Remove(0, 1) : prop;
            string acLayerName = $"{layer}${objLayerName}";

            var nestedObjects = FlattenCommitObject(@base[prop], converter, acLayerName, state, ref count, foundConvertibleMember);
            if (nestedObjects.Count > 0)
            {
              objects.AddRange(nestedObjects);
              foundConvertibleMember = true;
            }
          }
          if (!foundConvertibleMember && count == totalMembers) // this was an unsupported geo
            converter.Report.Log($"Skipped not supported type: { @base.speckle_type }. Object {@base.id} not baked.");
          return objects;
        }
      }

      if (obj is IReadOnlyList<object> list)
      {
        count = 0;
        foreach (var listObj in list)
          objects.AddRange(FlattenCommitObject(listObj, converter, layer, state, ref count));
        return objects;
      }

      if (obj is IDictionary dict)
      {
        count = 0;
        foreach (DictionaryEntry kvp in dict)
          objects.AddRange(FlattenCommitObject(kvp.Value, converter, layer, state, ref count));
        return objects;
      }

      return objects;
    }

    private void DeleteBlocksWithPrefix(string prefix, Transaction tr)
    {
      BlockTable blockTable = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
      foreach (ObjectId blockId in blockTable)
      {
        BlockTableRecord block = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
        if (block.Name.StartsWith(prefix))
        {
          block.UpgradeOpen();
          block.Erase();
        }
      }
    }

    private void DeleteLayersWithPrefix(string prefix, Transaction tr)
    {
      // Open the Layer table for read
      var lyrTbl = (LayerTable)tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead);
      foreach (ObjectId layerId in lyrTbl)
      {
        LayerTableRecord layer = (LayerTableRecord)tr.GetObject(layerId, OpenMode.ForRead);
        string layerName = layer.Name;
        if (layerName.StartsWith(prefix))
        {
          layer.UpgradeOpen();

          // cannot delete current layer: swap current layer to default layer "0" if current layer is to be deleted
          if (Doc.Database.Clayer == layerId)
          {
            var defaultLayerID = lyrTbl["0"];
            Doc.Database.Clayer = defaultLayerID;
          }
          layer.IsLocked = false;

          // delete all objects on this layer
          // TODO: this is ugly! is there a better way to delete layer objs instead of looping through each one?
          var bt = (BlockTable)tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead);
          foreach (var btId in bt)
          {
            var block = (BlockTableRecord)tr.GetObject(btId, OpenMode.ForRead);
            foreach (var entId in block)
            {
              var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
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
    }

    private bool GetOrMakeLayer(string layerName, Transaction tr, out string cleanName)
    {
      cleanName = Utils.RemoveInvalidChars(layerName);
      try
      {
        LayerTable layerTable = tr.GetObject(Doc.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
        if (layerTable.Has(cleanName))
        {
          return true;
        }
        else
        {
          layerTable.UpgradeOpen();
          var _layer = new LayerTableRecord();

          // Assign the layer properties
          _layer.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByColor, 7); // white
          _layer.Name = cleanName;

          // Append the new layer to the layer table and the transaction
          layerTable.Add(_layer);
          tr.AddNewlyCreatedDBObject(_layer, true);
        }
      }
      catch { return false; }
      return true;
    }

    #endregion

    #region sending
    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Utils.VersionedAppName);
      var streamId = state.StreamId;
      var client = state.Client;

      if (state.Filter != null)
        state.SelectedObjectIds = GetObjectsFromFilter(state.Filter, converter);

      // remove deleted object ids
      var deletedElements = new List<string>();
      foreach (var handle in state.SelectedObjectIds)
        if (Doc.Database.TryGetObjectId(Utils.GetHandle(handle), out ObjectId id))
          if (id.IsErased || id.IsNull)
            deletedElements.Add(handle);
      state.SelectedObjectIds = state.SelectedObjectIds.Where(o => !deletedElements.Contains(o)).ToList();

      if (state.SelectedObjectIds.Count == 0)
      {
        progress.Report.LogOperationError(new Exception("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."));
        return null;
      }

      var commitObject = new Base();
      commitObject["units"] = Utils.GetUnits(Doc); // TODO: check whether commits base needs units attached

      int convertedCount = 0;

      // invoke conversions on the main thread via control
      if (Control.InvokeRequired)
        Control.Invoke(new Action(() => ConvertSendCommit(commitObject, converter, state, progress, ref convertedCount)), new object[] { });
      else
        ConvertSendCommit(commitObject, converter, state, progress, ref convertedCount);

      progress.Report.Merge(converter.Report);

      if (convertedCount == 0)
      {
        progress.Report.LogOperationError(new SpeckleException("Zero objects converted successfully. Send stopped.", false));
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var commitObjId = await Operations.Send(
        commitObject,
        progress.CancellationTokenSource.Token,
        transports,
        onProgressAction: dict => progress.Update(dict),
        onErrorAction: (err, exception) =>
        {
          progress.Report.LogOperationError(exception);
          progress.CancellationTokenSource.Cancel();
        },
        disposeTransports: true
        );

      if (progress.Report.OperationErrorsCount != 0)
        return null;

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = commitObjId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {convertedCount} elements from {Utils.AppName}.",
        sourceApplication = Utils.VersionedAppName
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);
        state.PreviousCommitId = commitId;
        return commitId;
      }
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
      }
      return null;
    }

    delegate void SendingDelegate(Base commitObject, ISpeckleConverter converter, StreamState state, ProgressViewModel progress, ref int convertedCount);
    private void ConvertSendCommit(Base commitObject, ISpeckleConverter converter, StreamState state, ProgressViewModel progress, ref int convertedCount)
    {
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        // set the context doc for conversion - this is set inside the transaction loop because the converter retrieves this transaction for all db editing when the context doc is set!
        converter.SetContextDocument(Doc);

        var conversionProgressDict = new ConcurrentDictionary<string, int>();
        conversionProgressDict["Conversion"] = 0;

        bool renamedlayers = false;

        foreach (var autocadObjectHandle in state.SelectedObjectIds)
        {
          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
          {
            tr.Commit();
            return;
          }

          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);

          // get the db object from id
          Handle hn = Utils.GetHandle(autocadObjectHandle);
          DBObject obj = hn.GetObject(tr, out string type, out string layer);

          if (obj == null)
          {
            progress.Report.Log($"Skipped not found object: ${autocadObjectHandle}.");
            continue;
          }

          if (!converter.CanConvertToSpeckle(obj))
          {
            progress.Report.Log($"Skipped not supported type: ${type}. Object ${obj.Id} not sent.");
            continue;
          }

          try
          {
            // convert obj
            Base converted = null;
            string containerName = string.Empty;
            converted = converter.ConvertToSpeckle(obj);
            if (converted == null)
            {
              progress.Report.LogConversionError(new Exception($"Failed to convert object {autocadObjectHandle} of type {type}."));
              continue;
            }

            /* TODO: adding the extension dictionary / xdata per object 
            foreach (var key in obj.ExtensionDictionary)
              converted[key] = obj.ExtensionDictionary.GetUserString(key);
            */

#if CIVIL2021 || CIVIL2022 || CIVIL2023
            // add property sets if this is Civil3D
            var propertySets = obj.GetPropertySets(tr);
            if (propertySets.Count > 0)
              converted["propertySets"] = propertySets;
#endif

            if (obj is BlockReference)
              containerName = "Blocks";
            else
            {
              // remove invalid chars from layer name
              string cleanLayerName = Utils.RemoveInvalidDynamicPropChars(layer);
              containerName = cleanLayerName;
              if (!cleanLayerName.Equals(layer))
                renamedlayers = true;
            }

            if (commitObject[$"@{containerName}"] == null)
              commitObject[$"@{containerName}"] = new List<Base>();
            ((List<Base>)commitObject[$"@{containerName}"]).Add(converted);

            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);

            converted.applicationId = autocadObjectHandle;
          }
          catch (Exception e)
          {
            progress.Report.LogConversionError(new Exception($"Failed to convert object {autocadObjectHandle} of type {type}: {e.Message}"));
          }
          convertedCount++;
        }

        if (renamedlayers)
          progress.Report.Log("Replaced illegal chars ./ with - in one or more layer names.");

        tr.Commit();
      }
    }

    private List<string> GetObjectsFromFilter(ISelectionFilter filter, ISpeckleConverter converter)
    {
      var selection = new List<string>();
      switch (filter.Slug)
      {
        case "manual":
          return filter.Selection;
        case "all":
          return Doc.ConvertibleObjects(converter);
        case "layer":
          foreach (var layerName in filter.Selection)
          {
            TypedValue[] layerType = new TypedValue[1] { new TypedValue((int)DxfCode.LayerName, layerName) };
            PromptSelectionResult prompt = Doc.Editor.SelectAll(new SelectionFilter(layerType));
            if (prompt.Status == PromptStatus.OK)
              selection.AddRange(prompt.Value.GetHandles());
          }
          return selection;
      }
      return selection;
    }
    #endregion

    #region events
    public void RegisterAppEvents()
    {
      //// GLOBAL EVENT HANDLERS
      Application.DocumentWindowCollection.DocumentWindowActivated += Application_WindowActivated;
      Application.DocumentManager.DocumentActivated += Application_DocumentActivated;
      Doc.BeginDocumentClose += Application_DocumentClosed;

      var layers = Application.UIBindings.Collections.Layers;
      layers.CollectionChanged += Application_LayerChanged;
    }

    private void Application_LayerChanged(object sender, EventArgs e)
    {
      if (UpdateSelectedStream != null)
        UpdateSelectedStream();
    }

    //checks whether to refresh the stream list in case the user changes active view and selects a different document
    private void Application_WindowActivated(object sender, DocumentWindowActivatedEventArgs e)
    {
      try
      {
        if (e.DocumentWindow.Document == null || UpdateSavedStreams == null)
          return;

        var streams = GetStreamsInFile();
        UpdateSavedStreams(streams);

        MainViewModel.GoHome();
      }
      catch { }
    }

    private void Application_DocumentClosed(object sender, DocumentBeginCloseEventArgs e)
    {
      try
      {
        // Triggered just after a request is received to close a drawing.
        if (Doc != null)
          return;

        if (SpeckleAutocadCommand.MainWindow != null)
          SpeckleAutocadCommand.MainWindow.Hide();

        MainViewModel.GoHome();
      }
      catch { }
    }

    private void Application_DocumentActivated(object sender, DocumentCollectionEventArgs e)
    {
      try
      {
        // Triggered when a document window is activated. This will happen automatically if a document is newly created or opened.
        if (e.Document == null)
          return;

        var streams = GetStreamsInFile();
        if (streams.Count > 0)
          SpeckleAutocadCommand.CreateOrFocusSpeckle();

        if (UpdateSavedStreams != null)
          UpdateSavedStreams(streams);

        MainViewModel.GoHome();
      }
      catch { }
    }
    #endregion
  }
}
