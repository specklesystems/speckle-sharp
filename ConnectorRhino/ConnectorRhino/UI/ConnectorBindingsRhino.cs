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
using System.Windows.Threading;
using System.Timers;
using Rhino.Display;

namespace SpeckleRhino
{
  public partial class ConnectorBindingsRhino : ConnectorBindings
  {
    public RhinoDoc Doc { get => RhinoDoc.ActiveDoc; }

    public Timer SelectionTimer;

    private static string SpeckleKey = "speckle";

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
      Doc.Strings.SetString(SpeckleKey, state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override void RemoveStreamFromFile(string streamId)
    {
      Doc.Strings.Delete(SpeckleKey, streamId);
    }

    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      Doc.Strings.SetString(SpeckleKey, state.Stream.id, JsonConvert.SerializeObject(state));
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var strings = Doc?.Strings.GetEntryNames(SpeckleKey);
      if (strings == null)
      {
        return new List<StreamState>();
      }

      var states = strings.Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue(SpeckleKey, s))).ToList();

      if (states != null)
      {
        states.ForEach(x => x.Initialise(true));
      }

      return states;
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

    public override string GetHostAppName() => Applications.Rhino7;

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
      var layers = Doc.Layers.ToList().Where(layer => !layer.IsDeleted).Select(layer => layer.FullPath).ToList();
      var projectInfo = new List<string> { "Named Views" };

      return new List<ISelectionFilter>()
      {
        new ListSelectionFilter {Slug="layer", Name = "Layers", Icon = "LayersTriple", Description = "Selects objects based on their layers.", Values = layers },
        new ListSelectionFilter {Slug="project-info", Name = "P. Info", Icon = "Information", Values = projectInfo, Description="Adds the selected project information as views to the stream"},
        new AllSelectionFilter { Slug="all", Name = "All", Icon = "CubeScan", Description = "Selects all document objects and project info." }
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
      var converter = kit.LoadConverter(Applications.Rhino7);

      if (converter == null)
      {
        RaiseNotification($"Could not find any Kit!");
        state.CancellationTokenSource.Cancel();
        return null;
      }

      converter.SetContextDocument(Doc);

      var stream = await state.Client.StreamGet(state.Stream.id);

      if (state.CancellationTokenSource.Token.IsCancellationRequested)
      {
        return null;
      }

      var transport = new ServerTransport(state.Client.Account, state.Stream.id);

      Exceptions.Clear();

      string referencedObject = state.Commit.referencedObject;

      var commitId = state.Commit.id;
      //if "latest", always make sure we get the latest commit when the user clicks "receive"
      if (commitId == "latest")
      {
        var res = await state.Client.BranchGet(state.CancellationTokenSource.Token, state.Stream.id, state.Branch.name, 1);
        var commit = res.commits.items.FirstOrDefault();
        commitId = commit.id;
        referencedObject = commit.referencedObject;
      }

      var commitObject = await Operations.Receive(
        referencedObject,
        state.CancellationTokenSource.Token,
        transport,
        onProgressAction: d => UpdateProgress(d, state.Progress),
        onTotalChildrenCountKnown: num => Execute.PostToUIThread(() => state.Progress.Maximum = num),
        onErrorAction: (message, exception) => { Exceptions.Add(exception); },
        disposeTransports: true
        );

      if (Exceptions.Count != 0)
      {
        RaiseNotification($"Encountered some errors: {Exceptions.Last().Message}");
      }

      var undoRecord = Doc.BeginUndoRecord($"Speckle bake operation for {stream.name}");

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      Execute.PostToUIThread(() => state.Progress.Maximum = state.SelectedObjectIds.Count());

      Action updateProgressAction = () =>
      {
        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);
      };

      // get commit layer name 
      var commitLayerName = Speckle.DesktopUI.Utils.Formatting.CommitInfo(stream.name, state.Branch.name, commitId);

      // give converter a way to access the base commit layer name
      RhinoDoc.ActiveDoc.Notes += "%%%" + commitLayerName;

      var existingLayer = Doc.Layers.FindName(commitLayerName);
      if (existingLayer != null)
        Doc.Layers.Purge(existingLayer.Id, false);
      
      // flatten the commit object to retrieve children objs
      int count = 0;
      var commitObjs = FlattenCommitObject(commitObject, converter, commitLayerName, state, ref count);

      foreach (var commitObj in commitObjs)
      {
        var (obj, layerPath) = commitObj;
        BakeObject(obj, layerPath, state, converter);
        updateProgressAction?.Invoke();
      }

      Doc.Views.Redraw();
      Doc.EndUndoRecord(undoRecord);

      // undo notes edit
      var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
      Doc.Notes = segments[0];

      return state;
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
          if (@base.GetMembers().ContainsKey("displayMesh")) // add display mesh to member list if it exists
            props.Add("displayMesh");
          else if (@base.GetMembers().ContainsKey("displayValue"))
            props.Add("displayValue");
          int totalMembers = props.Count;

          foreach (var prop in props)
          {
            count++;

            // get bake layer name
            string objLayerName = prop.StartsWith("@") ? prop.Remove(0, 1) : prop;
            string rhLayerName = $"{layer}{Layer.PathSeparator}{objLayerName}";

            var nestedObjects = FlattenCommitObject(@base[prop], converter, rhLayerName, state, ref count, foundConvertibleMember);
            if (nestedObjects.Count > 0)
            {
              objects.AddRange(nestedObjects);
              foundConvertibleMember = true;
            }
          }

          if (!foundConvertibleMember && count == totalMembers) // this was an unsupported geo
          {
            state.Errors.Add(new Exception($"Receiving {@base.speckle_type} objects is not supported. Object {@base.id} not baked."));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
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

    // conversion and bake
    private void BakeObject(Base obj, string layerPath, StreamState state, ISpeckleConverter converter)
    {
      var converted = converter.ConvertToNative(obj);
      var convertedRH = converted as Rhino.Geometry.GeometryBase;

      if (convertedRH != null)
      {
        if (convertedRH.IsValid)
        {
          Layer bakeLayer = Doc.GetLayer(layerPath, true);
          if (bakeLayer != null)
          {
            var attributes = new ObjectAttributes { LayerIndex = bakeLayer.Index };

            // handle display
            Base display = obj[@"displayStyle"] as Base;
            if (display != null)
            {
              var color = display["color"] as int?;
              var lineStyle = display["linetype"] as string;
              var lineWidth = display["lineweight"] as double?;

              if (color != null)
              {
                attributes.ColorSource = ObjectColorSource.ColorFromObject;
                attributes.ObjectColor = System.Drawing.Color.FromArgb((int)color);
              }
              if (lineWidth != null)
                attributes.PlotWeight = (double)lineWidth;
              if (lineStyle != null)
              {
                var ls = Doc.Linetypes.FindName(lineStyle);
                if (ls != null)
                {
                  attributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
                  attributes.LinetypeIndex = ls.Index;
                }
              }
            }
            /* Not implemented since revit displaymesh objs do not have render materials attached
            else
            {
              Base render = obj[@"renderMaterial"] as Base;
              if (render != null)
              {
                var color = render["diffuse"] as int?;

                if (color != null)
                {
                  attributes.ColorSource = ObjectColorSource.ColorFromObject;
                  attributes.ObjectColor = System.Drawing.Color.FromArgb((int)color);
                }
              }
            }
            */

            // handle schema
            string schema = obj["SpeckleSchema"] as string;
            if (schema != null)
              attributes.SetUserString("SpeckleSchema", schema);

            if (Doc.Objects.Add(convertedRH, attributes) == Guid.Empty)
              state.Errors.Add(new Exception($"Failed to bake object {obj.id} of type {obj.speckle_type}."));
          }
          else
            state.Errors.Add(new Exception($"Could not create layer {layerPath} to bake objects into."));
        }
        else
          state.Errors.Add(new Exception($"Failed to bake object {obj.id} of type {obj.speckle_type}: invalid object props"));
      }
      else if (converted == null)
      {
        state.Errors.Add(new Exception($"Failed to convert object {obj.id} of type {obj.speckle_type}."));
      }
    }

    #endregion

    #region sending

    public override async Task<StreamState> SendStream(StreamState state)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.Rhino7);
      converter.SetContextDocument(Doc);
      Exceptions.Clear();

      var commitObj = new Base();
      
      int objCount = 0;
      bool renamedlayers = false;

      if (state.Filter != null)
      {
        state.SelectedObjectIds = GetObjectsFromFilter(state.Filter);
      }
      else
      {
        // remove object ids of any objects that may have been deleted
        state.SelectedObjectIds = state.SelectedObjectIds.Where(o => Doc.Objects.FindId(new Guid(o)) != null).ToList();
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

        Base converted = null;
        string containerName = string.Empty;

        try
        {
          RhinoObject obj = Doc.Objects.FindId(new Guid(applicationId)); // try get geom object
          if (obj != null)
          {
            if (!converter.CanConvertToSpeckle(obj))
            {
              state.Errors.Add(new Exception($"Objects of type ${obj.Geometry.ObjectType} are not supported"));
              continue;
            }
            converted = converter.ConvertToSpeckle(obj);
            if (converted == null)
            {
              state.Errors.Add(new Exception($"Failed to convert object ${applicationId} of type ${obj.Geometry.ObjectType}."));
              continue;
            }

            foreach (var key in obj.Attributes.GetUserStrings().AllKeys)
                converted[key] = obj.Attributes.GetUserString(key);

            if (obj is InstanceObject)
              containerName = "Blocks";
            else
            {
              var layerPath = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
              string cleanLayerPath = RemoveInvalidDynamicPropChars(layerPath);
              containerName = cleanLayerPath;
              if (!cleanLayerPath.Equals(layerPath))
                renamedlayers = true;
            }
          }
        }
        catch
        {
          int viewIndex = Doc.NamedViews.FindByName(applicationId); // try get view
          ViewInfo view = (viewIndex >= 0) ? Doc.NamedViews[viewIndex] : null; 
          if (view != null)
          {
            converted = converter.ConvertToSpeckle(view);
          }
          else
          {
            state.Errors.Add(new Exception($"Failed to find local view ${applicationId}."));
            continue;
          }
          if (converted == null)
          {
            state.Errors.Add(new Exception($"Failed to convert object ${applicationId} of type ${view.GetType()}."));
            continue;
          }
          containerName = "Named Views";
        }

        if (commitObj[$"@{containerName}"] == null)
          commitObj[$"@{containerName}"] = new List<Base>();
        ((List<Base>)commitObj[$"@{containerName}"]).Add(converted);

        conversionProgressDict["Conversion"]++;
        UpdateProgress(conversionProgressDict, state.Progress);

        // set application ids, also set for speckle schema base object if it exists
        converted.applicationId = applicationId;
        if (converted["@SpeckleSchema"] != null)
        {
          var newSchemaBase = converted["@SpeckleSchema"] as Base;
          newSchemaBase.applicationId = applicationId;
          converted["@SpeckleSchema"] = newSchemaBase;
        }

        objCount++;
      }

      if (objCount == 0)
      {
        RaiseNotification("Zero objects converted successfully. Send stopped.");
        return state;
      }

      if (renamedlayers)
        RaiseNotification("Replaced illegal chars ./ with - in one or more layer names.");

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
        onErrorAction: (err, exception) => { Exceptions.Add(exception); },
        disposeTransports: true
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
        sourceApplication = Applications.Rhino7
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
      var objs = new List<string>();

      switch (filter.Slug)
      {
        case "all":
          objs = Doc.Objects.Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList();
          objs.AddRange(Doc.NamedViews.Select(o => o.Name).ToList());
          break;
        case "layer":
          foreach (var layerPath in filter.Selection)
          {
            Layer layer = Doc.GetLayer(layerPath);
            if (layer != null && layer.IsVisible)
            {
              var layerObjs = Doc.Objects.FindByLayer(layer)?.Select(o => o.Id.ToString());
              if (layerObjs != null)
                objs.AddRange(layerObjs);
            }
          }
          break;
        case "project-info":
          if (filter.Selection.Contains("Named Views"))
            objs.AddRange(Doc.NamedViews.Select(o => o.Name).ToList());
          break;
        default:
          RaiseNotification("Filter type is not supported in this app. Why did the developer implement it in the first place?");
          break;
      }

      return objs;
    }

    private string RemoveInvalidDynamicPropChars(string str)
    {
      // remove ./
      return Regex.Replace(str, @"[./]", "-");
    }

    #endregion

  }
}
