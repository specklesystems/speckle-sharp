using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Transports;
using static DesktopUI2.ViewModels.MappingViewModel;
using static Speckle.ConnectorAutocadCivil.Utils;

#if ADVANCESTEEL2023
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
#endif

namespace Speckle.ConnectorAutocadCivil.UI
{
  public partial class ConnectorBindingsAutocad : ConnectorBindings
  {
    public static Document Doc => Application.DocumentManager.MdiActiveDocument;

    private static string ApplicationIdKey = "applicationId";

    /// <summary>
    /// Stored Base objects from commit flattening on receive: key is the Base id
    /// </summary>
    public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();

    /// <summary>
    /// Stored document line types used for baking objects on receive
    /// </summary>
    public Dictionary<string, ObjectId> LineTypeDictionary = new Dictionary<string, ObjectId>();

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

    public ConnectorBindingsAutocad()
      : base()
    {
      Control = new System.Windows.Forms.Control();
      Control.CreateControl();
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Create, ReceiveMode.Update };
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
    public override string GetHostAppNameVersion() =>
      Utils.VersionedAppName
        .Replace("AutoCAD", "AutoCAD ")
        .Replace("Civil3D", "Civil 3D ")
        .Replace("AdvanceSteel", "Advance Steel "); //hack for ADSK store;

    public override string GetHostAppName() => Utils.Slug;

    private string GetDocPath(Document doc) =>
      HostApplicationServices.Current.FindFile(doc?.Name, doc?.Database, FindFileHint.Default);

    public override string GetDocumentId()
    {
      string path = null;
      try
      {
        path = GetDocPath(Doc);
      }
      catch { }
      var docString = $"{(path != null ? path : "")}{(Doc != null ? Doc.Name : "")}";
      var hash = !string.IsNullOrEmpty(docString)
        ? Core.Models.Utilities.hashString(docString, Core.Models.Utilities.HashingFuctions.MD5)
        : null;
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
        new ListSelectionFilter
        {
          Slug = "layer",
          Name = "Layers",
          Icon = "LayersTriple",
          Description = "Selects objects based on their layers.",
          Values = GetLayers()
        },
        new AllSelectionFilter
        {
          Slug = "all",
          Name = "Everything",
          Icon = "CubeScan",
          Description = "Selects all document objects."
        }
      };
    }

    private List<ISetting> CurrentSettings { get; set; } // used to store the Stream State settings when sending/receiving

    // CAUTION: these strings need to have the same values as in the converter
    const string InternalOrigin = "Internal Origin (default)";
    const string UCS = "Current User Coordinate System";

    public override List<ISetting> GetSettings()
    {
      List<string> referencePoints = new List<string>() { InternalOrigin };

      // add the current UCS if it exists
      if (Doc.Editor.CurrentUserCoordinateSystem != null)
        referencePoints.Add(UCS);

      // add any named UCS if they exist
      var namedUCS = new List<string>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        var UCSTable = tr.GetObject(Doc.Database.UcsTableId, OpenMode.ForRead) as UcsTable;
        foreach (var entry in UCSTable)
        {
          var ucs = tr.GetObject(entry, OpenMode.ForRead) as UcsTableRecord;
          namedUCS.Add(ucs.Name);
        }
        tr.Commit();
      }
      if (namedUCS.Any())
        referencePoints.AddRange(namedUCS);

      return new List<ISetting>
      {
        new ListBoxSetting
        {
          Slug = "reference-point",
          Name = "Reference Point",
          Icon = "LocationSearching",
          Values = referencePoints,
          Selection = InternalOrigin,
          Description = "Sends or receives stream objects in relation to this document point"
        },
      };
    }

    //TODO
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override void SelectClientObjects(List<string> args, bool deselect = false)
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var currentSelection = editor.SelectImplied().Value?.GetObjectIds()?.ToList() ?? new List<ObjectId>();
      foreach (var arg in args)
      {
        try
        {
          if (Utils.GetHandle(arg, out Handle handle))
            if (Doc.Database.TryGetObjectId(handle, out ObjectId id))
            {
              if (deselect)
              {
                if (currentSelection.Contains(id))
                  currentSelection.Remove(id);
              }
              else
              {
                if (!currentSelection.Contains(id))
                  currentSelection.Add(id);
              }
            }
        }
        catch { }
      }
      if (currentSelection.Count == 0)
        editor.SetImpliedSelection(new ObjectId[0]);
      else
        Autodesk.AutoCAD.Internal.Utils.SelectObjects(currentSelection.ToArray());
      Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
    }

    public override void ResetDocument()
    {
      Doc.Editor.SetImpliedSelection(new ObjectId[0]);
      Autodesk.AutoCAD.Internal.Utils.FlushGraphics();
    }

    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(
      Dictionary<string, List<MappingValue>> Mapping
    )
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      return new Dictionary<string, List<MappingValue>>();
    }

    #endregion

    #region receiving
    public override bool CanPreviewReceive => false;

    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      return null;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      if (Doc == null)
        throw new InvalidOperationException("No Document is open");

      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);

      var stream = await state.Client.StreamGet(state.StreamId);

      Commit commit = await ConnectorHelpers.GetCommitFromState(progress.CancellationToken, state);
      state.LastCommit = commit;

      Base commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
      await ConnectorHelpers.TryCommitReceived(progress.CancellationToken, state, commit, Utils.VersionedAppName);

      // invoke conversions on the main thread via control
      try
      {
        if (Control.InvokeRequired)
          Control.Invoke(
            new ReceivingDelegate(ConvertReceiveCommit),
            commitObject,
            converter,
            state,
            progress,
            stream,
            commit.id
          );
        else
          ConvertReceiveCommit(commitObject, converter, state, progress, stream, commit.id);
      }
      catch (Exception ex)
      {
        throw new Exception($"Could not convert commit: {ex.Message}", ex);
      }

      return state;
    }

    delegate void ReceivingDelegate(
      Base commitObject,
      ISpeckleConverter converter,
      StreamState state,
      ProgressViewModel progress,
      Stream stream,
      string id
    );

    private void ConvertReceiveCommit(
      Base commitObject,
      ISpeckleConverter converter,
      StreamState state,
      ProgressViewModel progress,
      Stream stream,
      string id
    )
    {
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          // set the context doc for conversion - this is set inside the transaction loop because the converter retrieves this transaction for all db editing when the context doc is set!
          converter.SetContextDocument(Doc);
          converter.ReceiveMode = state.ReceiveMode;

          // set converter settings as tuples (setting slug, setting selection)
          var settings = new Dictionary<string, string>();
          CurrentSettings = state.Settings;
          foreach (var setting in state.Settings)
            settings.Add(setting.Slug, setting.Selection);
          converter.SetConverterSettings(settings);

          // keep track of conversion progress here
          progress.Report = new ProgressReport();
          var conversionProgressDict = new ConcurrentDictionary<string, int>();
          conversionProgressDict["Conversion"] = 0;

          // create a commit prefix: used for layers and block definition names
          var commitPrefix = DesktopUI2.Formatting.CommitInfo(stream.name, state.BranchName, id);

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
            converter.Report.LogOperationError(
              new Exception(
                $"Failed to remove existing layers or blocks starting with {commitPrefix} before importing new geometry."
              )
            );
          }

          // clear previously stored objects
          StoredObjects.Clear();

          // flatten the commit object to retrieve children objs
          var commitObjs = FlattenCommitObject(commitObject, converter);

          // open model space block table record for write
          BlockTableRecord btr = (BlockTableRecord)tr.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite);

          // Get doc line types for bake: more efficient this way than doing this per object
          LineTypeDictionary.Clear();
          var lineTypeTable = (LinetypeTable)tr.GetObject(Doc.Database.LinetypeTableId, OpenMode.ForRead);
          foreach (ObjectId lineTypeId in lineTypeTable)
          {
            var linetype = (LinetypeTableRecord)tr.GetObject(lineTypeId, OpenMode.ForRead);
            LineTypeDictionary.Add(linetype.Name, lineTypeId);
          }

          // conversion
          foreach (var commitObj in commitObjs)
          {
            // handle user cancellation
            if (progress.CancellationToken.IsCancellationRequested)
              return;

            // convert base (or base fallback values) and store in appobj converted prop
            if (commitObj.Convertible)
            {
              converter.Report.Log(commitObj); // Log object so converter can access
              try
              {
                commitObj.Converted = ConvertObject(commitObj, converter);
              }
              catch (Exception e)
              {
                commitObj.Log.Add($"Failed conversion: {e.Message}");
              }
            }
            else
              foreach (var fallback in commitObj.Fallback)
              {
                try
                {
                  fallback.Converted = ConvertObject(fallback, converter);
                }
                catch (Exception e)
                {
                  commitObj.Log.Add($"Fallback {fallback.applicationId} failed conversion: {e.Message}");
                }
                commitObj.Log.AddRange(fallback.Log);
              }

            // if the object wasnt converted, log fallback status
            if (commitObj.Converted == null || commitObj.Converted.Count == 0)
            {
              var convertedFallback = commitObj.Fallback.Where(o => o.Converted != null || o.Converted.Count > 0);
              if (convertedFallback != null && convertedFallback.Count() > 0)
                commitObj.Update(logItem: $"Creating with {convertedFallback.Count()} fallback values");
              else
                commitObj.Update(
                  status: ApplicationObject.State.Failed,
                  logItem: $"Couldn't convert object or any fallback values"
                );
            }

            // add to progress report
            progress.Report.Log(commitObj);
          }
          progress.Report.Merge(converter.Report);

          // add applicationID xdata before bake
          if (!ApplicationIdManager.AddApplicationIdXDataToDoc(Doc, tr))
          {
            progress.Report.LogOperationError(new Exception("Could not create document application id reg table"));
            return;
          }

          // handle operation errors
          if (progress.Report.OperationErrorsCount != 0)
            return;

          // bake
          var fileNameHash = GetDocumentId();
          foreach (var commitObj in commitObjs)
          {
            // handle user cancellation
            if (progress.CancellationToken.IsCancellationRequested)
              return;

            // find existing doc objects if they exist
            var existingObjs = new List<ObjectId>();
            var layer = commitObj.Container;
            switch (state.ReceiveMode)
            {
              case ReceiveMode.Update: // existing objs will be removed if it exists in the received commit
                existingObjs = ApplicationIdManager.GetObjectsByApplicationId(
                  Doc,
                  tr,
                  commitObj.applicationId,
                  fileNameHash
                );
                break;
              default:
                layer = $"{commitPrefix}${commitObj.Container}";
                break;
            }

            // bake
            if (commitObj.Convertible)
            {
              BakeObject(commitObj, converter, tr, layer, existingObjs);
              commitObj.Status = !commitObj.CreatedIds.Any()
                ? ApplicationObject.State.Failed
                : existingObjs.Count > 0
                  ? ApplicationObject.State.Updated
                  : ApplicationObject.State.Created;
            }
            else
            {
              foreach (var fallback in commitObj.Fallback)
                BakeObject(fallback, converter, tr, layer, existingObjs, commitObj);
              commitObj.Status =
                commitObj.Fallback.Where(o => o.Status == ApplicationObject.State.Failed).Count()
                == commitObj.Fallback.Count
                  ? ApplicationObject.State.Failed
                  : existingObjs.Count > 0
                    ? ApplicationObject.State.Updated
                    : ApplicationObject.State.Created;
            }
            Autodesk.AutoCAD.Internal.Utils.FlushGraphics();

            // log to progress report and update progress
            progress.Report.Log(commitObj);
            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);
          }

          // remove commit info from doc userdata
          Doc.UserData.Remove("commit");

          tr.Commit();
        }
      }
    }

    private List<ApplicationObject> FlattenCommitObject(Base obj, ISpeckleConverter converter)
    {
      //TODO: this implementation is almost identical to Rhino, we should try and extract as much of it as we can into Core
      void StoreObject(Base @base, ApplicationObject appObj)
      {
        if (StoredObjects.ContainsKey(@base.id))
          appObj.Update(logItem: "Found another object in this commit with the same id. Skipped other object"); //TODO check if we are actually ignoring duplicates, since we are returning the app object anyway...
        else
          StoredObjects.Add(@base.id, @base);
      }

      ApplicationObject CreateApplicationObject(Base current, string containerId)
      {
        ApplicationObject NewAppObj()
        {
          var speckleType = current.speckle_type
            .Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
          return new ApplicationObject(current.id, speckleType)
          {
            applicationId = current.applicationId,
            Container = containerId
          };
        }

        // skip if it is the base commit collection
        if (current.speckle_type.Contains("Collection") && string.IsNullOrEmpty(containerId))
          return null;

        //Handle convertable objects
        if (converter.CanConvertToNative(current))
        {
          var appObj = NewAppObj();
          appObj.Convertible = true;
          StoreObject(current, appObj);
          return appObj;
        }

        //Handle objects convertable using displayValues
        var fallbackMember = current["displayValue"] ?? current["@displayValue"];
        if (fallbackMember != null)
        {
          var appObj = NewAppObj();
          var fallbackObjects = GraphTraversal
            .TraverseMember(fallbackMember)
            .Select(o => CreateApplicationObject(o, containerId));
          appObj.Fallback.AddRange(fallbackObjects);

          StoreObject(current, appObj);
          return appObj;
        }

        return null;
      }

      string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
      StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
      {
        if (context.propName == null)
          return stringBuilder;

        string objectLayerName = string.Empty;
        if (context.propName.ToLower() == "elements" && context.current.speckle_type.Contains("Collection"))
        {
          objectLayerName = context.current["name"] as string;
        }
        else if (context.propName.ToLower() != "elements") // this is for any other property on the collection. skip elements props in layer structure.
        {
          objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;
        }
        LayerIdRecurse(context.parent, stringBuilder);
        if (stringBuilder.Length != 0 && !string.IsNullOrEmpty(objectLayerName))
        {
          stringBuilder.Append('$');
        }
        stringBuilder.Append(objectLayerName);

        return stringBuilder;
      }

      var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

      var objectsToConvert = traverseFunction
        .Traverse(obj)
        .Select(tc => CreateApplicationObject(tc.current, LayerId(tc)))
        .Where(appObject => appObject != null)
        .Reverse() //just for the sake of matching the previous behaviour as close as possible
        .ToList();

      return objectsToConvert;
    }

    private List<object> ConvertObject(ApplicationObject appObj, ISpeckleConverter converter)
    {
      var obj = StoredObjects[appObj.OriginalId];
      var convertedList = new List<object>();

      var converted = converter.ConvertToNative(obj);
      if (converted == null)
        return convertedList;

      //Iteratively flatten any lists
      void FlattenConvertedObject(object item)
      {
        if (item is IList list)
          foreach (object child in list)
            FlattenConvertedObject(child);
        else
          convertedList.Add(item);
      }
      FlattenConvertedObject(converted);

      return convertedList;
    }

    private void BakeObject(
      ApplicationObject appObj,
      ISpeckleConverter converter,
      Transaction tr,
      string layer,
      List<ObjectId> toRemove,
      ApplicationObject parent = null
    )
    {
      var obj = StoredObjects[appObj.OriginalId];
      int bakedCount = 0;
      bool remove =
        appObj.Status == ApplicationObject.State.Created
        || appObj.Status == ApplicationObject.State.Updated
        || appObj.Status == ApplicationObject.State.Failed
          ? false
          : true;

      foreach (var convertedItem in appObj.Converted)
      {
        switch (convertedItem)
        {
          case Entity o:

            if (o == null)
              continue;

            if (GetOrMakeLayer(layer, tr, out string cleanName))
            {
              var res = o.Append(cleanName);
              if (res.IsValid)
              {
                // handle display - fallback to rendermaterial if no displaystyle exists
                Base display = obj[@"displayStyle"] as Base;
                if (display == null)
                  display = obj[@"renderMaterial"] as Base;
                if (display != null)
                  Utils.SetStyle(display, o, LineTypeDictionary);

                // add property sets if this is Civil3D
#if CIVIL2021 || CIVIL2022 || CIVIL2023
                try
                {
                  if (obj["propertySets"] is IReadOnlyList<object> list)
                  {
                    var propertySets = new List<Dictionary<string, object>>();
                    foreach (var listObj in list)
                      propertySets.Add(listObj as Dictionary<string, object>);
                    o.SetPropertySets(Doc, propertySets);
                  }
                }
                catch (Exception e)
                {
                  appObj.Log.Add($"Could not attach property sets: {e.Message}");
                }
#endif

                // set application id
                var appId = parent != null ? parent.applicationId : obj.applicationId;
                var newObj = tr.GetObject(res, OpenMode.ForWrite);
                if (!ApplicationIdManager.SetObjectCustomApplicationId(newObj, appId, out appId))
                {
                  appObj.Log.Add($"Could not attach applicationId xdata");
                }

                tr.TransactionManager.QueueForGraphicsFlush();

                if (parent != null)
                  parent.Update(createdId: res.Handle.ToString());
                else
                  appObj.Update(createdId: res.Handle.ToString());

                bakedCount++;
              }
              else
              {
                var bakeMessage = $"Could not bake to document.";
                if (parent != null)
                  parent.Update(logItem: $"fallback {appObj.applicationId}: {bakeMessage}");
                else
                  appObj.Update(logItem: bakeMessage);
                continue;
              }
            }
            else
            {
              var layerMessage = $"Could not create layer {layer}.";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.applicationId}: {layerMessage}");
              else
                appObj.Update(logItem: layerMessage);
              continue;
            }
            break;
          default:
            break;
        }
      }

      if (bakedCount == 0)
      {
        if (parent != null)
          parent.Update(logItem: $"fallback {appObj.applicationId}: could not bake object");
        else
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not bake object");
      }
      else
      {
        // remove existing objects if they exist
        if (remove)
        {
          foreach (var objId in toRemove)
          {
            try
            {
              DBObject objToRemove = tr.GetObject(objId, OpenMode.ForWrite);
              objToRemove.Erase();
            }
            catch (Exception e)
            {
              if (!e.Message.Contains("eWasErased")) // this couldve been previously received and deleted
              {
                if (parent != null)
                  parent.Log.Add(e.Message);
                else
                  appObj.Log.Add(e.Message);
              }
            }
          }
          appObj.Status = toRemove.Count > 0 ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
        }
      }
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
      catch
      {
        return false;
      }
      return true;
    }

    #endregion

    #region sending
    public override bool CanPreviewSend => true;

    public override async void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      // report and converter
      progress.Report = new ProgressReport();
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);
      if (converter == null)
      {
        progress.Report.LogOperationError(new Exception("Could not load converter"));
        return;
      }
      converter.SetContextDocument(Doc);

      var filterObjs = GetObjectsFromFilter(state.Filter, converter);
      var existingIds = new List<string>();
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        foreach (var id in filterObjs)
        {
          DBObject obj = null;
          string type = "";
          if (Utils.GetHandle(id, out Handle hn))
          {
            obj = hn.GetObject(tr, out type, out string layer, out string applicationId);
          }
          if (obj == null)
          {
            progress.Report.Log(
              new ApplicationObject(id, "unknown")
              {
                Status = ApplicationObject.State.Failed,
                Log = new List<string>() { "Could not find object in document" }
              }
            );
            continue;
          }

          var appObj = new ApplicationObject(id, type) { Status = ApplicationObject.State.Unknown };

          if (converter.CanConvertToSpeckle(obj))
          {
            appObj.Update(status: ApplicationObject.State.Created);
          }
          else
          {
#if ADVANCESTEEL2023
            UpdateASObject(appObj, obj);
#endif
            appObj.Update(
              status: ApplicationObject.State.Failed,
              logItem: "Object type conversion to Speckle not supported"
            );
          }

          progress.Report.Log(appObj);
          existingIds.Add(id);
        }
        tr.Commit();
      }

      if (existingIds.Count == 0)
      {
        progress.Report.LogOperationError(new Exception("No valid objects selected, nothing will be sent!"));
        return;
      }

      Doc.Editor.SetImpliedSelection(new ObjectId[0]);
      SelectClientObjects(existingIds);
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.VersionedAppName);

      var streamId = state.StreamId;
      var client = state.Client;
      progress.Report = new ProgressReport();

      if (state.Filter != null)
        state.SelectedObjectIds = GetObjectsFromFilter(state.Filter, converter);

      // remove deleted object ids
      var deletedElements = new List<string>();
      foreach (var selectedId in state.SelectedObjectIds)
        if (Utils.GetHandle(selectedId, out Handle handle))
          if (Doc.Database.TryGetObjectId(handle, out ObjectId id))
            if (id.IsErased || id.IsNull)
              deletedElements.Add(selectedId);

      state.SelectedObjectIds = state.SelectedObjectIds.Where(o => !deletedElements.Contains(o)).ToList();

      if (state.SelectedObjectIds.Count == 0)
      {
        throw new InvalidOperationException(
          "Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something."
        );
      }

      var commitObject = new Base();
      commitObject["units"] = Utils.GetUnits(Doc); // TODO: check whether commits base needs units attached

      int convertedCount = 0;

      // invoke conversions on the main thread via control
      try
      {
        if (Control.InvokeRequired)
          Control.Invoke(
            new Action(() => ConvertSendCommit(commitObject, converter, state, progress, ref convertedCount)),
            new object[] { }
          );
        else
          ConvertSendCommit(commitObject, converter, state, progress, ref convertedCount);
        progress.Report.Merge(converter.Report);
      }
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
      }

      if (convertedCount == 0)
      {
        throw new SpeckleException("Zero objects converted successfully. Send stopped.");
      }

      progress.CancellationToken.ThrowIfCancellationRequested();

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var commitObjId = await Operations.Send(
        commitObject,
        progress.CancellationToken,
        transports,
        onProgressAction: dict => progress.Update(dict),
        onErrorAction: ConnectorHelpers.DefaultSendErrorHandler,
        disposeTransports: true
      );

      progress.CancellationToken.ThrowIfCancellationRequested();

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = commitObjId,
        branchName = state.BranchName,
        message = state.CommitMessage ?? $"Pushed {convertedCount} elements from {Utils.AppName}.",
        sourceApplication = Utils.VersionedAppName
      };

      if (state.PreviousCommitId != null)
      {
        actualCommit.parents = new List<string>() { state.PreviousCommitId };
      }

      var commitId = await ConnectorHelpers.CreateCommit(progress.CancellationToken, client, actualCommit);
      return commitId;
    }

    delegate void SendingDelegate(
      Base commitObject,
      ISpeckleConverter converter,
      StreamState state,
      ProgressViewModel progress,
      ref int convertedCount
    );

    private void ConvertSendCommit(
      Base commitObject,
      ISpeckleConverter converter,
      StreamState state,
      ProgressViewModel progress,
      ref int convertedCount
    )
    {
      using (DocumentLock acLckDoc = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          // set the context doc for conversion - this is set inside the transaction loop because the converter retrieves this transaction for all db editing when the context doc is set!
          converter.SetContextDocument(Doc);

          // set converter settings as tuples (setting slug, setting selection)
          var settings = new Dictionary<string, string>();
          CurrentSettings = state.Settings;
          foreach (var setting in state.Settings)
            settings.Add(setting.Slug, setting.Selection);
          converter.SetConverterSettings(settings);

          var conversionProgressDict = new ConcurrentDictionary<string, int>();
          conversionProgressDict["Conversion"] = 0;

          // add applicationID xdata before send
          if (!ApplicationIdManager.AddApplicationIdXDataToDoc(Doc, tr))
          {
            progress.Report.LogOperationError(new Exception("Could not create document application id reg table"));
            return;
          }

          // get the hash of the file name to create a more unique application id
          var fileNameHash = GetDocumentId();

          string servicedApplication = converter.GetServicedApplications().First();

          foreach (var autocadObjectHandle in state.SelectedObjectIds)
          {
            // handle user cancellation
            if (progress.CancellationToken.IsCancellationRequested)
            {
              return;
            }

            // get the db object from id
            DBObject obj = null;
            string layer = null;
            string applicationId = null;
            if (Utils.GetHandle(autocadObjectHandle, out Handle hn))
            {
              obj = hn.GetObject(tr, out string type, out layer, out applicationId);
            }
            else
            {
              progress.Report.LogOperationError(new Exception($"Failed to find doc object ${autocadObjectHandle}."));
              continue;
            }

            // create applicationobject for reporting
            Base converted = null;
            var descriptor = Utils.ObjectDescriptor(obj);
            ApplicationObject reportObj = new ApplicationObject(autocadObjectHandle, descriptor)
            {
              applicationId = autocadObjectHandle
            };

            if (!converter.CanConvertToSpeckle(obj))
            {
#if ADVANCESTEEL2023
              UpdateASObject(reportObj, obj);
#endif
              reportObj.Update(
                status: ApplicationObject.State.Skipped,
                logItem: $"Sending this object type is not supported in {Utils.AppName}"
              );
              progress.Report.Log(reportObj);
              continue;
            }

            try
            {
              // convert obj
              converter.Report.Log(reportObj); // Log object so converter can access
              converted = converter.ConvertToSpeckle(obj);
              if (converted == null)
              {
                reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
                progress.Report.Log(reportObj);
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

              string containerName = obj is BlockReference ? "Blocks" : Utils.RemoveInvalidDynamicPropChars(layer); // remove invalid chars from layer name

              if (commitObject[$"@{containerName}"] == null)
                commitObject[$"@{containerName}"] = new List<Base>();
              ((List<Base>)commitObject[$"@{containerName}"]).Add(converted);

              // set application id
              #region backwards compatibility
              // this is just to overwrite old files with objects that have the unappended autocad native application id
              bool isOldApplicationId(string appId)
              {
                if (string.IsNullOrEmpty(appId))
                  return false;
                return appId.Length == 5 ? true : false;
              }
              if (isOldApplicationId(applicationId))
              {
                ApplicationIdManager.SetObjectCustomApplicationId(
                  obj,
                  autocadObjectHandle,
                  out applicationId,
                  fileNameHash
                );
              }
              #endregion

              if (applicationId == null) // this object didn't have an xdata appId field
              {
                if (
                  !ApplicationIdManager.SetObjectCustomApplicationId(
                    obj,
                    autocadObjectHandle,
                    out applicationId,
                    fileNameHash
                  )
                )
                {
                  reportObj.Log.Add("Could not set application id xdata");
                }
              }
              converted.applicationId = applicationId;

              // update progress
              conversionProgressDict["Conversion"]++;
              progress.Update(conversionProgressDict);

              // log report object
              reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.GetType().Name}");
              progress.Report.Log(reportObj);

              convertedCount++;
            }
            catch (Exception e)
            {
              //TODO: Log to serilog failed conversions
              reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"{e.Message}");
              progress.Report.Log(reportObj);
              continue;
            }
          }

          tr.Commit();
        }
      }
    }

#if ADVANCESTEEL2023
    private void UpdateASObject(ApplicationObject applicationObject, DBObject obj)
    {
      if (!CheckAdvanceSteelObject(obj))
        return;

      ASFilerObject filerObject = GetFilerObjectByEntity<ASFilerObject>(obj);
      if (filerObject != null)
      {
        applicationObject.Update(descriptor: filerObject.GetType().Name);
      }
    }
#endif

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

    private void Application_DocumentActivated(object sender, DocumentCollectionEventArgs e)
    {
      try
      {
        // Triggered when a document window is activated. This will happen automatically if a document is newly created or opened.
        if (e.Document == null)
        {
          if (SpeckleAutocadCommand.MainWindow != null)
            SpeckleAutocadCommand.MainWindow.Hide();

          MainViewModel.GoHome();
          return;
        }

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
