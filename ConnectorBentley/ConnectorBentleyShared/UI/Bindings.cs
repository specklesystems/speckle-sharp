using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Collections;

using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.ConnectorBentley.Entry;
using Speckle.ConnectorBentley.Storage;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.MstnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;

#if (OPENBUILDINGS)
using Bentley.Building.Api;
#endif

#if (OPENROADS || OPENRAIL || OPENBRIDGE)
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.SDK;
#endif

using Stylet;

namespace Speckle.ConnectorBentley.UI
{
  public partial class ConnectorBindingsBentley : ConnectorBindings
  {
    public DgnFile File => Session.Instance.GetActiveDgnFile();
    public DgnModel Model => Session.Instance.GetActiveDgnModel();
    public string ModelUnits { get; set; }
#if (OPENROADS || OPENRAIL || OPENBRIDGE)
    public GeometricModel GeomModel { get; private set; }
    public List<string> civilElementKeys => new List<string> { "Alignment" };
#endif
    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();
    public List<Exception> Exceptions { get; set; } = new List<Exception>();
    public List<Exception> ConversionErrors { get; set; } = new List<Exception>();
#if (OPENBUILDINGS)
    public bool ExportGridLines { get; set; } = true;
#else
    public bool ExportGridLines = false;
#endif
    /// <summary>
    /// Keeps track of errors in the operations of send/receive.
    /// </summary>
    public List<Exception> OperationErrors { get; set; } = new List<Exception>();

    // Like the AutoCAD API, the Bentley APIs should only be called on the main thread.
    // As in the AutoCAD/Civil3D connectors, we therefore creating a control in the ConnectorBindings constructor (since it's called on main thread) that allows for invoking worker threads on the main thread - thank you Claire!!
    public System.Windows.Forms.Control Control;
    public ConnectorBindingsBentley() : base()
    {
      Control = new System.Windows.Forms.Control();
      Control.CreateControl();

      ModelUnits = Model.GetModelInfo().GetMasterUnit().GetName(true, true);

#if (OPENROADS || OPENRAIL || OPENBRIDGE)
      ConsensusConnection sdkCon = Bentley.CifNET.SDK.Edit.ConsensusConnectionEdit.GetActive();
      GeomModel = sdkCon.GetActiveGeometricModel();
#endif
    }

    /// <summary>
    /// Adds a new stream to the file.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override void AddNewStream(StreamState state)
    {
      var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
      if (index == -1)
      {
        DocumentStreams.Add(state);
        WriteStateToFile();
      }
    }

    public override string GetActiveViewName()
    {
      string viewName = Session.GetActiveViewport().GetViewName();
      return viewName;
    }

    public override string GetDocumentId()
    {
      return Core.Models.Utilities.hashString("X" + File.GetFileName(), Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation()
    {
      return Path.GetDirectoryName(File.GetFileName());
    }

    public override string GetFileName()
    {
      return Path.GetFileName(File.GetFileName());
    }

    public override string GetHostAppName() => Utils.VersionedAppName;

    public override List<string> GetObjectsInView()
    {
      if (Model == null)
      {
        return new List<string>();
      }

      var graphicElements = Model.GetGraphicElements();

      var objs = new List<string>();
      using (var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator())
      {
        objs = graphicElements.Where(el => !el.IsInvisible).Select(el => el.ElementId.ToString()).ToList(); // Note: this returns all graphic objects in the model.
      }

      return objs;
    }

    public override List<string> GetSelectedObjects()
    {
      var objs = new List<string>();

      if (Model == null)
      {
        return objs;
      }

      uint numSelected = SelectionSetManager.NumSelected();
      DgnModelRef modelRef = Session.Instance.GetActiveDgnModelRef();

      for (uint i = 0; i < numSelected; i++)
      {
        Bentley.DgnPlatformNET.Elements.Element el = null;
        SelectionSetManager.GetElement(i, ref el, ref modelRef);
        objs.Add(el.ElementId.ToString());
      }

      return objs;
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      //Element Type, Element Class, Element Template, Material, Level, Color, Line Style, Line Weight
      var levels = new List<string>();

      FileLevelCache levelCache = Model.GetFileLevelCache();
      foreach (var level in levelCache.GetHandles())
      {
        levels.Add(level.Name);
      }
      levels.Sort();

      var elementTypes = new List<string> { "Arc", "Ellipse", "Line", "Spline", "Line String", "Complex Chain", "Shape", "Complex Shape", "Mesh" };

      var filterList = new List<ISelectionFilter>();
      filterList.Add(new AllSelectionFilter { Slug = "all", Name = "Everything", Icon = "CubeScan", Description = "Selects all document objects." });
      filterList.Add(new ListSelectionFilter { Slug = "level", Name = "Levels", Icon = "LayersTriple", Description = "Selects objects based on their level.", Values = levels });
      filterList.Add(new ListSelectionFilter { Slug = "elementType", Name = "Element Types", Icon = "Category", Description = "Selects objects based on their element type.", Values = elementTypes });

#if (OPENROADS || OPENRAIL || OPENBRIDGE)
      var civilElementTypes = new List<string> { "Alignment" };
      filterList.Add(new ListSelectionFilter { Slug = "civilElementType", Name = "Civil Features", Icon = "RailroadVariant", Description = "Selects civil features based on their type.", Values = civilElementTypes });
#endif



      return filterList;
    }

    private void UpdateProgress(ConcurrentDictionary<string, int> dict, DesktopUI.Utils.ProgressReport progress)
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

    delegate ECSchema ListSchemaDelegate();
    delegate List<StreamState> ReadSchemaDelegate(ECSchema schema);

    public override List<StreamState> GetStreamsInFile()
    {
      if (File != null)
      {
        ECSchema schema;
        if (Control.InvokeRequired)
        {
          schema = (ECSchema)Control.Invoke(new ListSchemaDelegate(StreamStateManager.StreamStateListSchema.GetSchema));
          DocumentStreams = (List<StreamState>)Control.Invoke(new ReadSchemaDelegate(StreamStateManager.ReadState), new object[] { schema });
        }
        else
        {
          schema = StreamStateManager.StreamStateListSchema.GetSchema();
          if (schema == null) schema = StreamStateManager.StreamStateListSchema.AddSchema();
          DocumentStreams = StreamStateManager.ReadState(schema);
        }
      }

      return DocumentStreams;
    }

    delegate void WriteStateDelegate(List<StreamState> DocumentStreams);

    /// <summary>
    /// Transaction wrapper around writing the local streams to the file.
    /// </summary>
    private void WriteStateToFile()
    {
      if (Control.InvokeRequired)
        Control.Invoke(new WriteStateDelegate(StreamStateManager.WriteStreamStateList), new object[] { DocumentStreams });
      else
        StreamStateManager.WriteStreamStateList(DocumentStreams);
    }

    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
      if (index != -1)
      {
        DocumentStreams[index] = state;
        WriteStateToFile();
      }
    }

    public override async Task<StreamState> ReceiveStream(StreamState state)
    {
      ConversionErrors.Clear();
      OperationErrors.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Utils.VersionedAppName);
      if (converter == null)
      {
        RaiseNotification($"Could not find any Kit!");
        state.CancellationTokenSource.Cancel();
        return null;
      }

      if (Control.InvokeRequired)
        Control.Invoke(new SetContextDelegate(converter.SetContextDocument), new object[] { Session.Instance });
      else
        converter.SetContextDocument(Session.Instance);

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
          onTotalChildrenCountKnown: count => Execute.PostToUIThread(() => state.Progress.Maximum = count)
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

      var flattenedObjects = FlattenCommitObject(commitObject, converter);

      List<ApplicationPlaceholderObject> newPlaceholderObjects;
      if (Control.InvokeRequired)
        newPlaceholderObjects = (List<ApplicationPlaceholderObject>)Control.Invoke(new NativeConversionAndBakeDelegate(ConvertAndBakeReceivedObjects), new object[] { flattenedObjects, converter, state });
      else
        newPlaceholderObjects = ConvertAndBakeReceivedObjects(flattenedObjects, converter, state);

      // receive was cancelled by user
      if (newPlaceholderObjects == null)
      {
        converter.Report.ConversionErrors.Add(new Exception("fatal error: receive cancelled by user"));
        return null;
      }

      DeleteObjects(previouslyReceiveObjects, newPlaceholderObjects);

      state.ReceivedObjects = newPlaceholderObjects;

      state.Errors.AddRange(converter.Report.ConversionErrors);

      if (converter.Report.ConversionErrors.Any(x => x.Message.Contains("fatal error")))
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
    private void DeleteObjects(List<ApplicationPlaceholderObject> previouslyReceiveObjects, List<ApplicationPlaceholderObject> newPlaceholderObjects)
    {
      foreach (var obj in previouslyReceiveObjects)
      {
        if (newPlaceholderObjects.Any(x => x.applicationId == obj.applicationId))
          continue;

        // get the model object from id               
        ulong id = Convert.ToUInt64(obj.ApplicationGeneratedId);
        var element = Model.FindElementById((ElementId)id);
        if (element != null)
        {
          element.DeleteFromModel();
        }
      }
    }

    private string GetItemTypeProperty(Element host, string libraryName, string itemTypeName, string propertyName)
    {
      CustomItemHost itemHost = new CustomItemHost(host, false);
      IDgnECInstance ecInstance = itemHost.GetCustomItem(libraryName, itemTypeName);
      if (ecInstance != null)
      {
        var prop = ecInstance.GetAsString(propertyName);
        return prop;
      }
      return null;
    }

    private Element FindExistingElementByApplicationId(ISpeckleConverter converter, string applicationId)
    {
      var modelObjIds = Model.ConvertibleObjects(converter);
      foreach (var objId in modelObjIds)
      {
        double.TryParse(objId, out double id);
        var obj = Model.FindElementById((ElementId)(long)id);
        var prop = GetItemTypeProperty(obj, "Speckle", "Speckle Data", "ApplicationId");

        if (prop == applicationId)
          return obj;
      }

      return null;
    }

    delegate List<ApplicationPlaceholderObject> NativeConversionAndBakeDelegate(List<Base> objects, ISpeckleConverter converter, StreamState state);
    private List<ApplicationPlaceholderObject> ConvertAndBakeReceivedObjects(List<Base> objects, ISpeckleConverter converter, StreamState state)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
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
          SpeckleBentleyCommand.Bootstrapper.RootWindow.Dispatcher.Invoke(() =>
          {
            UpdateProgress(conversionProgressDict, state.Progress);
          }, System.Windows.Threading.DispatcherPriority.Background);

          var convRes = converter.ConvertToNative(@base);

          if (convRes is ApplicationPlaceholderObject placeholder)
          {
            placeholders.Add(placeholder);
          }
          else if (convRes is List<ApplicationPlaceholderObject> placeholderList)
          {
            placeholders.AddRange(placeholderList);
          }

          var libraryName = "Speckle";
          var itemTypeName = "Speckle Data";
          var propertyName = "ApplicationId";

          // try to update existing, fall back to adding new elements if failed
          var convertedElement = convRes as Element;
          if (convertedElement != null && convertedElement.IsValid)
          {
            var status = StatusInt.Error;

            // check for existing speckle generated id in file ec data
            var existing = FindExistingElementByApplicationId(converter, @base.applicationId);
            if (existing != null)
            {
              status = convertedElement.ReplaceInModel(existing);
            }
            else
            {
              // check for existing bentley id 
              var parse = double.TryParse(@base.applicationId, out double id);
              if (parse)
              {
                var appId = (long)id;
                var existingElement = Model.FindElementById((ElementId)appId);

                if (existingElement != null)
                {
                  var oldElement = Element.GetFromElementRef(existingElement.GetNativeElementRef());

                  if (oldElement != null)
                  {
                    try
                    {
                      status = convertedElement.ReplaceInModel(oldElement);
                    }
                    catch
                    {
                      status = convertedElement.AddToModel();
                    }
                  }
                }
                else
                {
                  status = convertedElement.AddToModel();
                }
              }
              else
              {
                status = convertedElement.AddToModel();
              }
            }

            if (status == StatusInt.Error)
            {
              state.Errors.Add(new Exception($"Failed to bake object {@base.id} of type {@base.speckle_type}."));
            }
          }
          else
          {
            state.Errors.Add(new Exception($"Failed to convert object {@base.id} of type {@base.speckle_type}."));
          }

          // add item type property to track applicationId
          CustomItemHost customItemHost = new CustomItemHost(convertedElement, false);
          ItemTypeLibrary itemTypeLibrary = ItemTypeLibrary.FindByName(libraryName, File);
          ItemType itemType = null;

          if (itemTypeLibrary == null)
          {
            itemTypeLibrary = ItemTypeLibrary.Create(libraryName, File);
            itemType = itemTypeLibrary.AddItemType(itemTypeName);
            CustomProperty customProperty = itemType.AddProperty(propertyName);
            customProperty.DefaultValue = "applicationId";
            customProperty.Type = CustomProperty.TypeKind.String;
            itemTypeLibrary.Write();
          }
          else
          {
            itemType = itemTypeLibrary.GetItemTypeByName(itemTypeName);
            if (itemType == null)
              itemType = itemTypeLibrary.AddItemType(itemTypeName);

            CustomProperty customProperty = itemType.GetPropertyByName(propertyName);
            if (customProperty == null)
              customProperty = itemType.AddProperty(propertyName);
          }

          IDgnECInstance item = customItemHost.GetCustomItem(libraryName, itemTypeName);
          if (item == null)
          {
            item = customItemHost.ApplyCustomItem(itemType, true);
          }

          if (item != null)
          {
            item.SetString("ApplicationId", @base.applicationId);
            item.SetValue("ApplicationId", @base.applicationId);
            item.WriteChanges();
          }
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

    public override void RemoveStreamFromFile(string streamId)
    {
      var streamState = DocumentStreams.FirstOrDefault(s => s.Stream.id == streamId);
      if (streamState != null)
      {
        DocumentStreams.Remove(streamState);
        WriteStateToFile();
      }
    }

    public override void SelectClientObjects(string args)
    {
      throw new NotImplementedException();
    }

    delegate void SetContextDelegate(object session);
    delegate List<string> GetObjectsFromFilterDelegate(ISelectionFilter filter, ISpeckleConverter converter);
    delegate Base SpeckleConversionDelegate(object commitObject);

#if (OPENROADS || OPENRAIL || OPENBRIDGE)
    delegate List<NamedModelEntity> GetCivilObjectsDelegate(StreamState state);
    delegate string GetCivilObjectNameDelegate(object commitObject);
#endif

    public override async Task<StreamState> SendStream(StreamState state)
    {
      ConversionErrors.Clear();
      OperationErrors.Clear();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Utils.VersionedAppName);

      if (Control.InvokeRequired)
        Control.Invoke(new SetContextDelegate(converter.SetContextDocument), new object[] { Session.Instance });
      else
        converter.SetContextDocument(Session.Instance);

      var streamId = state.Stream.id;
      var client = state.Client;

      var selectedObjects = new List<Object>();

      if (state.Filter != null)
      {
        if (Control.InvokeRequired)
          state.SelectedObjectIds = (List<string>)Control.Invoke(new GetObjectsFromFilterDelegate(GetObjectsFromFilter), new object[] { state.Filter, converter });
        else
          state.SelectedObjectIds = GetObjectsFromFilter(state.Filter, converter);
      }

      if (state.SelectedObjectIds.Count == 0 && !ExportGridLines)
      {
        RaiseNotification("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.");
        return state;
      }

      var commitObj = new Base();

      var units = Units.GetUnitsFromString(ModelUnits).ToLower();
      commitObj["units"] = units;

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());
      int convertedCount = 0;

      // grab elements from active model           
      var objs = new List<Element>();
#if (OPENROADS || OPENRAIL || OPENBRIDGE)
      bool convertCivilObject = false;
      var civObjs = new List<NamedModelEntity>();

      if (civilElementKeys.Count(x => state.SelectedObjectIds.Contains(x)) > 0)
      {
        if (Control.InvokeRequired)
          civObjs = (List<NamedModelEntity>)Control.Invoke(new GetCivilObjectsDelegate(GetCivilObjects), new object[] { state });
        else
          civObjs = GetCivilObjects(state);

        objs = civObjs.Select(x => x.Element).ToList();
        convertCivilObject = true;
      }
      else
      {
        objs = state.SelectedObjectIds.Select(x => Model.FindElementById((ElementId)Convert.ToUInt64(x))).ToList();
      }
#else
      objs = state.SelectedObjectIds.Select(x => Model.FindElementById((ElementId)Convert.ToUInt64(x))).ToList();
#endif

#if (OPENBUILDINGS)
      if (ExportGridLines)
      {
        // grab grid lines
        ITFApplication appInst = new TFApplicationList();

        if (0 == appInst.GetProject(0, out ITFLoadableProjectList projList) && projList != null)
        {
          ITFLoadableProject proj = projList.AsTFLoadableProject;
          if (null == proj)
            return null;

          ITFDrawingGrid drawingGrid = null;
          if (Control.InvokeRequired)
            Control.Invoke((Action)(() => { proj.GetDrawingGrid(false, 0, out drawingGrid); }));
          else
            proj.GetDrawingGrid(false, 0, out drawingGrid);

          if (null == drawingGrid)
            return null;

          Base converted;
          if (Control.InvokeRequired)
            converted = (Base)Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { drawingGrid });
          else
            converted = converter.ConvertToSpeckle(drawingGrid);

          if (converted != null)
          {
            var containerName = "Grid Systems";

            if (commitObj[$"@{containerName}"] == null)
              commitObj[$"@{containerName}"] = new List<Base>();
            ((List<Base>)commitObj[$"@{containerName}"]).Add(converted);

            // not sure this makes much sense here
            conversionProgressDict["Conversion"]++;
            UpdateProgress(conversionProgressDict, state.Progress);

            //gridLine.applicationId = ??;

            convertedCount++;
          }
        }
      }
#endif

      foreach (var obj in objs)
      {
        if (state.CancellationTokenSource.Token.IsCancellationRequested)
        {
          return null;
        }

        if (obj == null)
        {
          state.Errors.Add(new Exception($"Failed to find local object."));
          continue;
        }

        var objId = obj.ElementId.ToString();
        var objType = obj.ElementType;

        if (!converter.CanConvertToSpeckle(obj))
        {
          state.Errors.Add(new Exception($"Objects of type ${objType} are not supported"));
          continue;
        }

        // convert obj
        // try catch to prevent memory access violation crash in case a conversion goes wrong
        Base converted = null;
        string containerName = string.Empty;
        try
        {
          var levelCache = Model.GetFileLevelCache();
          var objLevel = levelCache.GetLevel(obj.LevelId);
          var layerName = "Unknown";
          if (objLevel != null)
          {
            if (objLevel.Status != LevelCacheErrorCode.CannotFindLevel)
            {
              layerName = objLevel.Name;
            }
          }

#if (OPENROADS || OPENRAIL || OPENBRIDGE)
          if (convertCivilObject)
          {
            var civilObj = civObjs[objs.IndexOf(obj)];
            if (Control.InvokeRequired)
            {
              converted = (Base)Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { civilObj });
              Control.Invoke((Action)(() => { containerName = civilObj.Name == "" ? "Unnamed" : civilObj.Name; }));
            }
            else
            {
              converted = converter.ConvertToSpeckle(civilObj);
              containerName = civilObj.Name == "" ? "Unnamed" : civilObj.Name;
            }
          }
          else
          {
            if (Control.InvokeRequired)
              converted = (Base)Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { obj });
            else
              converted = converter.ConvertToSpeckle(obj);
            containerName = layerName;
          }
#else
          if (Control.InvokeRequired)
            converted = (Base)Control.Invoke(new SpeckleConversionDelegate(converter.ConvertToSpeckle), new object[] { obj });
          else
            converted = converter.ConvertToSpeckle(obj);

          containerName = layerName;
#endif
          if (converted == null)
          {
            state.Errors.Add(new Exception($"Failed to convert object ${objId} of type ${objType}."));
            continue;
          }
        }
        catch
        {
          state.Errors.Add(new Exception($"Failed to convert object {objId} of type {objType}."));
          continue;
        }

        /* TODO: adding the feature data and properties per object 
        foreach (var key in obj.ExtensionDictionary)
        {
          converted[key] = obj.ExtensionDictionary.GetUserString(key);
        }
        */

        if (commitObj[$"@{containerName}"] == null)
          commitObj[$"@{containerName}"] = new List<Base>();
        ((List<Base>)commitObj[$"@{containerName}"]).Add(converted);

        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);

        converted.applicationId = objId;
        //converted[""]

        convertedCount++;
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

      if (convertedCount > 0)
      {
        var actualCommit = new CommitCreateInput
        {
          streamId = streamId,
          objectId = commitObjId,
          branchName = state.Branch.name,
          message = state.CommitMessage != null ? state.CommitMessage : $"Pushed {convertedCount} elements from {Utils.AppName}.",
          sourceApplication = Utils.VersionedAppName
        };

        if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

        try
        {
          var commitId = await client.CommitCreate(actualCommit);

          await state.RefreshStream();
          state.PreviousCommitId = commitId;

          try
          {
            PersistAndUpdateStreamInFile(state);
          }
          catch (Exception e)
          {

          }
          RaiseNotification($"{convertedCount} objects sent to {state.Stream.name}.");
        }
        catch (Exception e)
        {
          Globals.Notify($"Failed to create commit.\n{e.Message}");
          state.Errors.Add(e);
        }
      }
      else
      {
        Globals.Notify($"Did not create commit: no objects could be converted.");
      }

      return state;
    }

#if (OPENROADS || OPENRAIL || OPENBRIDGE)
    private List<NamedModelEntity> GetCivilObjects(StreamState state)
    {
      var civilObjs = new List<NamedModelEntity>();
      foreach (var objId in state.SelectedObjectIds)
      {
        switch (objId)
        {
          case "Alignment":
            civilObjs.AddRange(GeomModel.Alignments);
            break;
          case "Corridor":
            civilObjs.AddRange(GeomModel.Corridors);
            break;
        }
      }
      return civilObjs;
    }
#endif

    private List<string> GetObjectsFromFilter(ISelectionFilter filter, ISpeckleConverter converter)
    {
      switch (filter.Slug)
      {
        case "all":
          return Model.ConvertibleObjects(converter);
        case "level":
          var levelObjs = new List<string>();
          foreach (var levelName in filter.Selection)
          {
            var levelCache = Model.GetFileLevelCache();
            var levelHandle = levelCache.GetLevelByName(levelName);
            var levelId = levelHandle.LevelId;

            var graphicElements = Model.GetGraphicElements();
            var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator();
            var objs = graphicElements.Where(el => el.LevelId == levelId).Select(el => el.ElementId.ToString()).ToList();
            levelObjs.AddRange(objs);
          }
          return levelObjs;
        case "elementType":
          var typeObjs = new List<string>();
          foreach (var typeName in filter.Selection)
          {
            MSElementType selectedType = MSElementType.None;
            switch (typeName)
            {
              case "Arc":
                selectedType = MSElementType.Arc;
                break;
              case "Ellipse":
                selectedType = MSElementType.Ellipse;
                break;
              case "Line":
                selectedType = MSElementType.Line;
                break;
              case "Spline":
                selectedType = MSElementType.BsplineCurve;
                break;
              case "Line String":
                selectedType = MSElementType.LineString;
                break;
              case "Complex Chain":
                selectedType = MSElementType.ComplexString;
                break;
              case "Shape":
                selectedType = MSElementType.Shape;
                break;
              case "Complex Shape":
                selectedType = MSElementType.ComplexShape;
                break;
              case "Mesh":
                selectedType = MSElementType.MeshHeader;
                break;
              case "Surface":
                selectedType = MSElementType.BsplineSurface;
                break;
              default:
                break;
            }
            var graphicElements = Model.GetGraphicElements();
            var elementEnumerator = (ModelElementsEnumerator)graphicElements.GetEnumerator();
            var objs = graphicElements.Where(el => el.ElementType == selectedType).Select(el => el.ElementId.ToString()).ToList();
            typeObjs.AddRange(objs);
          }
          return typeObjs;
#if (OPENROADS || OPENRAIL || OPENBRIDGE)
        case "civilElementType":
          var civilTypeObjs = new List<string>();
          foreach (var typeName in filter.Selection)
          {
            switch (typeName)
            {
              case "Alignment":
                var alignments = GeomModel.Alignments;
                if (alignments != null)
                  if (alignments.Count() > 0)
                    civilTypeObjs.Add("Alignment");
                break;
              case "Corridor":
                var corridors = GeomModel.Corridors;
                if (corridors != null)
                  if (corridors.Count() > 0)
                    civilTypeObjs.Add("Corridor");
                break;
              default:
                break;
            }
          }
          return civilTypeObjs;
#endif
        default:
          RaiseNotification("Filter type is not supported in this app. Why did the developer implement it in the first place?");
          return new List<string>();
      }
    }
  }
}
