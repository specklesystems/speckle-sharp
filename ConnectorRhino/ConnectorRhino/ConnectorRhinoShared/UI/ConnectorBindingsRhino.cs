using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using Serilog;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using static DesktopUI2.ViewModels.MappingViewModel;
using ApplicationObject = Speckle.Core.Models.ApplicationObject;

namespace SpeckleRhino
{
  public partial class ConnectorBindingsRhino : ConnectorBindings
  {
    public RhinoDoc Doc { get => RhinoDoc.ActiveDoc; }

    private static string SpeckleKey = "speckle2";
    private static string UserStrings = "userStrings";
    private static string UserDictionary = "userDictionary";
    private static string ApplicationIdKey = "applicationId";

    public Dictionary<string, Base> StoredObjects = new Dictionary<string, Base>();
    public Dictionary<string, Base> StoredObjectParams = new Dictionary<string, Base>(); // these are to store any parameters found on parent objects to add to fallback objects
    public List<ApplicationObject> Preview { get; set; } = new List<ApplicationObject>();
    public PreviewConduit PreviewConduit { get; set; }
    private string SelectedReceiveCommit { get; set; }

    public ConnectorBindingsRhino()
    {
      RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
      RhinoDoc.LayerTableEvent += RhinoDoc_LayerChange;
    }

    private void RhinoDoc_EndOpenDocument(object sender, DocumentOpenEventArgs e)
    {
      if (e.Merge)
        return; // prevents triggering this on copy pastes, imports, etc.

      if (e.Document == null)
        return;

      var streams = GetStreamsInFile();
      if (UpdateSavedStreams != null)
        UpdateSavedStreams(streams);
      //if (streams.Count > 0)
      //  SpeckleCommand.CreateOrFocusSpeckle();
    }

    private void RhinoDoc_LayerChange(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e)
    {
      if (UpdateSelectedStream != null)
        UpdateSelectedStream();
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Update, ReceiveMode.Create };
    }

    #region Local streams I/O with local file

    public override List<StreamState> GetStreamsInFile()
    {
      var strings = Doc?.Strings.GetEntryNames(SpeckleKey);

      if (strings == null)
        return new List<StreamState>();

      var states = strings.Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue(SpeckleKey, s))).ToList();
      return states;
    }

    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      Doc.Strings.Delete(SpeckleKey);
      foreach (var s in streams)
        Doc.Strings.SetString(SpeckleKey, s.StreamId, JsonConvert.SerializeObject(s));
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

    public override string GetHostAppNameVersion() => Utils.RhinoAppName;
    public override string GetHostAppName() => HostApplications.Rhino.Slug;

    public override string GetDocumentId()
    {
      return Speckle.Core.Models.Utilities.hashString("X" + Doc?.Path + Doc?.Name, Speckle.Core.Models.Utilities.HashingFuctions.MD5);
    }

    public override string GetDocumentLocation() => Doc?.Path;

    public override string GetFileName() => Doc?.Name;

    //improve this to add to log??
    private void LogUnsupportedObjects(List<RhinoObject> objs, ISpeckleConverter converter)
    {
      var reportLog = new Dictionary<string, int>();
      foreach (var obj in objs)
      {
        var type = obj.ObjectType.ToString();
        if (reportLog.ContainsKey(type)) reportLog[type] = reportLog[type]++;
        else reportLog.Add(type, 1);
      }
      RhinoApp.WriteLine("Deselected unsupported objects:");
      foreach (var entry in reportLog)
        Rhino.RhinoApp.WriteLine($"{entry.Value} of type {entry.Key}");
    }
    public override List<string> GetSelectedObjects()
    {
      var objs = new List<string>();
      var Converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);

      if (Converter == null || Doc == null) return objs;

      var selected = Doc.Objects.GetSelectedObjects(true, false).ToList();
      if (selected.Count == 0) return objs;
      var supportedObjs = selected.Where(o => Converter.CanConvertToSpeckle(o) == true)?.ToList();
      var unsupportedObjs = selected.Where(o => Converter.CanConvertToSpeckle(o) == false)?.ToList();

      // handle any unsupported objects and modify doc selection if so
      if (unsupportedObjs.Count > 0)
      {
        LogUnsupportedObjects(unsupportedObjs, Converter);
        Doc.Objects.UnselectAll(false);
        supportedObjs.ForEach(o => o.Select(true, true));
      }

      return supportedObjs.Select(o => o.Id.ToString())?.ToList();
    }

    public override List<ISelectionFilter> GetSelectionFilters()
    {
      var layers = Doc.Layers.ToList().Where(layer => !layer.IsDeleted).Select(layer => layer.FullPath).ToList();
      var projectInfo = new List<string> { "Named Views", "Standard Views" };

      return new List<ISelectionFilter>()
      {
        new AllSelectionFilter { Slug="all", Name = "Everything", Icon = "CubeScan", Description = "Selects all document objects and project info." },
        new ListSelectionFilter {Slug="layer", Name = "Layers", Icon = "LayersTriple", Description = "Selects objects based on their layers.", Values = layers },
        new ListSelectionFilter {Slug="project-info", Name = "Project Information", Icon = "Information", Values = projectInfo, Description="Adds the selected project information as views to the stream"},
        new ManualSelectionFilter()
      };
    }

    public override List<ISetting> GetSettings()
    {
      /*
      var referencePoints = new List<string>() { "Internal Origin (default)" };
      referencePoints.AddRange(Doc.NamedConstructionPlanes.Select(o => o.Name).ToList());
      return new List<ISetting>()
      {
        new ListBoxSetting {Slug = "reference-point", Name = "Reference Point", Icon ="LocationSearching", Values = referencePoints, Description = "Receives stream objects in relation to this document point"}
      };
      */
      return new List<ISetting>();
    }

    public override void SelectClientObjects(List<string> objs, bool deselect = false)
    {
      var isPreview = PreviewConduit != null && PreviewConduit.Enabled ? true : false;

      foreach (var id in objs)
      {
        RhinoObject obj = null;
        try
        {
          obj = Doc.Objects.FindId(new Guid(id)); // this is a rhinoobj
        }
        catch
        {
          continue; // this was a named view!
        }

        if (obj != null)
        {
          if (deselect) obj.Select(false, true, false, true, true, true);
          else obj.Select(true, true, true, true, true, true);
        }
        else if (isPreview)
        {
          PreviewConduit.Enabled = false;
          PreviewConduit.SelectPreviewObject(id, deselect);
          PreviewConduit.Enabled = true;
        }
      }

      Doc.Views.ActiveView.ActiveViewport.ZoomExtentsSelected();
      Doc.Views.Redraw();
    }

    public override void ResetDocument()
    {
      if (PreviewConduit != null)
        PreviewConduit.Enabled = false;
      else
        Doc.Objects.UnselectAll(false);

      Doc.Views.Redraw();
    }

    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(Dictionary<string, List<MappingValue>> Mapping)
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      return new Dictionary<string, List<MappingValue>>();
    }

    #endregion

    #region receiving 
    public override bool CanPreviewReceive => true;

    private static bool IsPreviewIgnore(Base @object) => @object.speckle_type.Contains("Instance") || @object.speckle_type.Contains("View");

    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      var cancellationToken = progress.CancellationTokenSource.Token;
      // first check if commit is the same and preview objects have already been generated
      Commit commit = await GetCommitFromState(cancellationToken, state);
      progress.Report = new ProgressReport();

      if (commit.id != SelectedReceiveCommit)
      {
        // check for converter 
        var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
        if (converter == null)
        {
          //TODO: Log warning (or throw)
          progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
          return null;
        }
        converter.SetContextDocument(Doc);

        var commitObject = await GetCommit(commit, state, progress);
        if (commitObject == null)
        {
          //TODO: Log warning (or throw)
          progress.Report.LogOperationError(new Exception($"Could not retrieve commit {commit.id} from server"));
          progress.CancellationTokenSource.Cancel();
        }

        SelectedReceiveCommit = commit.id;
        ClearStorage();

        var commitLayerName = DesktopUI2.Formatting.CommitInfo(state.CachedStream.name, state.BranchName, commit.id); // get commit layer name 
        Preview = FlattenCommitObject(commitObject, converter);
        Doc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info

        // Convert preview objects
        foreach (var previewObj in Preview)
        {
          previewObj.CreatedIds = new List<string>() { previewObj.OriginalId }; // temporary store speckle id as created id for Preview report selection to work

          var storedObj = StoredObjects[previewObj.OriginalId];
          if (IsPreviewIgnore(storedObj))
          {
            var status = previewObj.Convertible ? ApplicationObject.State.Created : ApplicationObject.State.Skipped;
            previewObj.Update(status: status, logItem: "No preview available");
            progress.Report.Log(previewObj);
            continue;
          }

          if (previewObj.Convertible)
            previewObj.Converted = ConvertObject(storedObj, converter);
          else
            foreach (var fallback in previewObj.Fallback)
            {
              var storedFallback = StoredObjects[fallback.OriginalId];
              fallback.Converted = ConvertObject(storedFallback, converter);
            }

          if (previewObj.Converted == null || previewObj.Converted.Count == 0)
          {
            var convertedFallback = previewObj.Fallback.Where(o => o.Converted != null || o.Converted.Count > 0);
            if (convertedFallback != null && convertedFallback.Count() > 0)
              previewObj.Update(status: ApplicationObject.State.Created, logItem: $"Creating with {convertedFallback.Count()} fallback values");
            else
              previewObj.Update(status: ApplicationObject.State.Failed, logItem: $"Couldn't convert object or any fallback values");
          }
          else
            previewObj.Status = ApplicationObject.State.Created;

          progress.Report.Log(previewObj);
        }
        progress.Report.Merge(converter.Report);

        // undo notes edit
        var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
        Doc.Notes = segments[0];
      }
      else // just generate the log
      {
        foreach (var previewObj in Preview)
          progress.Report.Log(previewObj);
      }

      // create display conduit
      try
      {
        PreviewConduit = new PreviewConduit(Preview);
      }
      catch (Exception e)
      {
        progress.Report.OperationErrors.Add(new Exception($"Could not create preview: {e.Message}"));
        ResetDocument();
        return null;
      }
      PreviewConduit.Enabled = true;
      Doc.Views.ActiveView.ActiveViewport.ZoomBoundingBox(PreviewConduit.bbox);
      Doc.Views.Redraw();

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        PreviewConduit.Enabled = false;
        ResetDocument();
        return null;
      }

      return state;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="state"></param>
    /// <param name="progress"></param>
    /// <exception cref="KitException">When the <see cref="KitManager.GetDefaultKit()"/> fails to successfully find/load a converter to serve <see cref="Utils.RhinoAppName"/></exception>>
    /// <exception cref="OperationCanceledException">When cancellation is requested via <paramref name="progress"/></exception>
    /// <returns></returns>
    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var cancellationToken = progress.CancellationTokenSource.Token;
      
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      converter.SetContextDocument(Doc);
      converter.ReceiveMode = state.ReceiveMode;

      Commit commit = await GetCommitFromState(cancellationToken, state);

      state.LastSourceApp = commit.sourceApplication;

      if (SelectedReceiveCommit != commit.id)
      {
        ClearStorage();
        SelectedReceiveCommit = commit.id;
      }

      progress.Report = new ProgressReport();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;
      
      
      var undoRecord = Doc.BeginUndoRecord($"Speckle bake operation for {state.CachedStream.name}");

      // get commit layer name
      var commitLayerName = DesktopUI2.Formatting.CommitInfo(state.CachedStream.name, state.BranchName, commit.id);

      Base commitObject = null;
      if (Preview.Count == 0)
        commitObject = await GetCommit(commit, state, progress);

      cancellationToken.ThrowIfCancellationRequested();
      RhinoApp.InvokeOnUiThread((Action)(() =>
      {
        RhinoDoc.ActiveDoc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info

        // create preview objects if they don't already exist
        if (Preview.Count == 0)
        {
          // flatten the commit object to retrieve children objs
          Preview = FlattenCommitObject(commitObject, converter);

          // convert
          foreach (var previewObj in Preview)
          {
            var isPreviewIgnore = false;
            if (previewObj.Convertible)
            {
              converter.Report.Log(previewObj); // Log object so converter can access
              var storedObj = StoredObjects[ previewObj.OriginalId ];
              if (storedObj == null)
              {
                previewObj.Update(status: ApplicationObject.State.Failed,
                  logItem: $"Couldn't retrieve stored object from bindings");
                continue;
              }

              isPreviewIgnore = IsPreviewIgnore(storedObj);
              if (!isPreviewIgnore)
              {
                previewObj.Converted = ConvertObject(storedObj, converter);
              }
            }
            else
            {
              foreach (var fallback in previewObj.Fallback)
              {
                var storedFallback = StoredObjects[ fallback.OriginalId ];
                fallback.Converted = ConvertObject(storedFallback, converter);
              }
            }

            if (!isPreviewIgnore && (previewObj.Converted == null || previewObj.Converted.Count == 0))
            {
              var convertedFallback =
                previewObj.Fallback.Where(o => o.Converted != null || o.Converted.Count > 0).ToList();
              if (convertedFallback.Any())
                previewObj.Update(logItem: $"Creating with {convertedFallback.Count()} fallback values");
              else
                previewObj.Update(status: ApplicationObject.State.Failed,
                  logItem: $"Couldn't convert object or any fallback values");
            }

            progress.Report.Log(previewObj);
            if (progress.CancellationTokenSource.Token.IsCancellationRequested)
              return;
          }

          progress.Report.Merge(converter.Report);
        }

        if (progress.Report.OperationErrorsCount != 0)
          return;

        foreach (var previewObj in Preview)
        {
          var isUpdate = false;

          // check receive mode & if objects need to be removed from the document after bake (or received objs need to be moved layers)
          var toRemove = new List<RhinoObject>();
          var layer = previewObj.Container;
          switch (state.ReceiveMode)
          {
            case ReceiveMode.Update: // existing objs will be removed if it exists in the received commit
              toRemove = GetObjectsByApplicationId(previewObj.applicationId);
              toRemove.ForEach(o => Doc.Objects.Delete(o));

              if (!toRemove.Any()) // if no rhinoobjects were found, this could've been a view
              {
                var viewId = Doc.NamedViews.FindByName(previewObj.applicationId);
                if (viewId != -1)
                {
                  isUpdate = true;
                  Doc.NamedViews.Delete(viewId);
                }
              }
              break;
            default:
              layer = $"{commitLayerName}{Layer.PathSeparator}{previewObj.Container}"; // use the commit as the top level layer in create mode
              break;
          }
          if (toRemove.Count() > 0) isUpdate = true;

          // bake

          previewObj.CreatedIds.Clear(); // clear created ids before bake because these may be speckle ids from the preview

          if (previewObj.Convertible)
          {
            BakeObject(previewObj, converter, layer);
            previewObj.Status = !previewObj.CreatedIds.Any() ? ApplicationObject.State.Failed :
            isUpdate ? ApplicationObject.State.Updated :
            ApplicationObject.State.Created;
          }
          else
          {
            foreach (var fallback in previewObj.Fallback)
              BakeObject(fallback, converter, layer, previewObj);
            previewObj.Status = previewObj.Fallback.Count(o => o.Status == ApplicationObject.State.Failed) == previewObj.Fallback.Count ?
              ApplicationObject.State.Failed : isUpdate ?
              ApplicationObject.State.Updated : ApplicationObject.State.Created;
          }

          progress.Report.Log(previewObj);

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            return;
          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);
        }
        progress.Report.Merge(converter.Report);

        // undo notes edit
        var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
        Doc.Notes = segments[0];
      }));

      Doc.Views.Redraw();
      Doc.EndUndoRecord(undoRecord);

      return state;
    }
    //#nullable disable
    // gets objects by id directly or by application id user string
    private List<RhinoObject> GetObjectsByApplicationId(string applicationId)
    {
      if (string.IsNullOrEmpty(applicationId))
        return new List<RhinoObject>();

      // first try to find the object by app id user string
      var match = Doc.Objects.Where(o => o.Attributes.GetUserString(ApplicationIdKey) == applicationId)?.ToList() ?? new List<RhinoObject>();

      // if nothing is found, look for the geom obj by its guid directly
      if (!match.Any())
      {
        try
        {
          RhinoObject obj = Doc.Objects.FindId(new Guid(applicationId));
          if (obj != null)
            match.Add(obj);
        }
        catch { }
      }
      return match;
    }

    // gets the state commit
    private static async Task<Commit> GetCommitFromState(CancellationToken cancellationToken, StreamState state)
    {
      cancellationToken.ThrowIfCancellationRequested();
      
      Commit commit;
      try
      {
        if (state.CommitId == "latest") //if "latest", always make sure we get the latest commit
        {
          var res = await state.Client.BranchGet(cancellationToken, state.StreamId, state.BranchName, 1);
          commit = res.commits.items.First();
        }
        else
        {
          var res = await state.Client.CommitGet(cancellationToken, state.StreamId, state.CommitId);
          commit = res;
        }
      }
      catch (OperationCanceledException)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to fetch requested commit id: {state.CommitId} from branch: \"{state.BranchName}\" from stream: {state.StreamId}", ex);
      }

      return commit;
    }


    /// <exception cref="Exception">Thrown when any receive operation errors</exception>
    private static async Task<Base> GetCommit(Commit commit, StreamState state, ProgressViewModel progress)
    {
      progress.CancellationTokenSource.Token.ThrowIfCancellationRequested();

      var transport = new ServerTransport(state.Client.Account, state.StreamId);
      
      var commitObject = await Operations.Receive(
        commit.referencedObject,
        progress.CancellationTokenSource.Token,
        transport,
        onProgressAction: dict => progress.Update(dict),
        onErrorAction: (s, e) =>
        {
          //Treat all operation errors as fatal
          
          //Often, exceptions are caused simply because we have cancled. So if we have, cancel          
          progress.CancellationTokenSource.Token.ThrowIfCancellationRequested();

          throw new Exception(s, e);
        },
        onTotalChildrenCountKnown: (c) => progress.Max = c,
        disposeTransports: true
      );
      
      return commitObject;
    }

    /// <summary>
    /// Traverses the object graph, returning objects to be converted.
    /// </summary>
    /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
    /// <param name="converter">The converter instance, used to define what objects are convertable</param>
    /// <returns>A flattened list of objects to be converted ToNative</returns>
    private List<ApplicationObject> FlattenCommitObject(Base obj, ISpeckleConverter converter)
    {

      void StoreObject(Base @base, ApplicationObject appObj, Base parameters = null)
      {
        if (StoredObjects.ContainsKey(@base.id))
          appObj.Update(
            logItem:
            "Found another object in this commit with the same id. Skipped other object"); //TODO check if we are actually ignoring duplicates, since we are returning the app object anyway...
        else
          StoredObjects.Add(@base.id, @base);

        if (parameters != null && !StoredObjectParams.ContainsKey(@base.id))
          StoredObjectParams.Add(@base.id, parameters);
      }
      
      ApplicationObject CreateApplicationObject(Base current, string containerId)
      {
        ApplicationObject NewAppObj()
        {
          var speckleType = current.speckle_type.Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();
          return new ApplicationObject(current.id, speckleType) { applicationId = current.applicationId, Container = containerId };
        }
        
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
        var parameters = current["parameters"] as Base;
        if (fallbackMember != null)
        {
          var appObj = NewAppObj();
          var fallbackObjects = GraphTraversal.TraverseMember(fallbackMember)
            .Select(o => CreateApplicationObject(o, containerId));
          appObj.Fallback.AddRange(fallbackObjects);

          StoreObject(current, appObj, parameters);
          return appObj;
        }
        
        return null;
      }
      
      string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
      StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
      {
        if (context.propName == null) return stringBuilder;

        var objectLayerName = context.propName[0] == '@'
          ? context.propName.Substring(1)
          : context.propName;

        LayerIdRecurse(context.parent, stringBuilder);
        stringBuilder.Append(Layer.PathSeparator);
        stringBuilder.Append(objectLayerName);
        
        return stringBuilder;
      }

      var traverseFunction = DefaultTraversal.CreateTraverseFunc(converter);

      var objectsToConvert = traverseFunction.Traverse(obj)
        .Select(tc => CreateApplicationObject(tc.current, LayerId(tc)))
        .Where(appObject => appObject != null)
        .Reverse() //just for the sake of matching the previous behaviour as close as possible
        .ToList();

      return objectsToConvert;
    }

    // conversion and bake
    private List<object> ConvertObject(Base obj, ISpeckleConverter converter)
    {
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
    private void BakeObject(ApplicationObject appObj, ISpeckleConverter converter, string layer, ApplicationObject parent = null)
    {
      var obj = StoredObjects[appObj.OriginalId];
      int bakedCount = 0;
      // check if this is a view or block - convert instead of bake if so (since these are "baked" during conversion)
      if (IsPreviewIgnore(obj))
      {
        appObj.Converted = ConvertObject(obj, converter);
      }
      foreach (var convertedItem in appObj.Converted)
      {
        switch (convertedItem)
        {
          case GeometryBase o:
            if (!o.IsValidWithLog(out string log))
            {
              var invalidMessage = $"{log.Replace("\n", "").Replace("\r", "")}";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.applicationId}: {invalidMessage}");
              else
                appObj.Update(logItem: invalidMessage);
              continue;
            }
            Layer bakeLayer = Doc.GetLayer(layer, true);
            if (bakeLayer == null)
            {
              var layerMessage = $"Could not create layer {layer}.";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.applicationId}: {layerMessage}");
              else
                appObj.Update(logItem: layerMessage);
              continue;
            }
            var attributes = new ObjectAttributes();

            // handle display style
            if (obj[@"displayStyle"] is Base display)
            {
              if (converter.ConvertToNative(display) is ObjectAttributes displayAttribute)
                attributes = displayAttribute;
            }
            else if (obj[@"renderMaterial"] is Base renderMaterial)
            {
              attributes.ColorSource = ObjectColorSource.ColorFromMaterial;
            }

            // assign layer
            attributes.LayerIndex = bakeLayer.Index;

            // handle user info, including application id
            SetUserInfo(obj, attributes, parent);

            Guid id = Doc.Objects.Add(o, attributes);
            if (id == Guid.Empty)
            {
              var bakeMessage = $"Could not bake to document.";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.applicationId}: {bakeMessage}");
              else
                appObj.Update(logItem: bakeMessage);
              continue;
            }

            if (parent != null)
              parent.Update(createdId: id.ToString());
            else
              appObj.Update(createdId: id.ToString());

            bakedCount++;

            // handle render material
            if (obj[@"renderMaterial"] is Base render)
            {
              var convertedMaterial = converter.ConvertToNative(render); //Maybe wrap in try catch in case no conversion exists?
              if (convertedMaterial is RenderMaterial rm)
              {
                var rhinoObject = Doc.Objects.FindId(id);
                rhinoObject.RenderMaterial = rm;
                rhinoObject.CommitChanges();
              }
            }
            break;
          case RhinoObject o: // this was prbly a block instance, baked during conversion
            if (parent != null)
              parent.Update(createdId: o.Id.ToString());
            else
              appObj.Update(createdId: o.Id.ToString());
            bakedCount++;
            break;
          case ViewInfo o: // this is a view, baked during conversion
            if (parent != null)
              parent.Update(createdId: o.Name);
            else
              appObj.Update(createdId: o.Name);
            bakedCount++;
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
          appObj.Update(logItem: $"Could not bake object");
      }
    }

    private void SetUserInfo(Base obj, ObjectAttributes attributes, ApplicationObject parent = null)
    {
      // set user strings
      if (obj[UserStrings] is Base userStrings)
        foreach (var member in userStrings.GetMembers(DynamicBaseMemberType.Dynamic))
          attributes.SetUserString(member.Key, member.Value as string);

      // set application id
      var appId = parent != null ? parent.applicationId : obj.applicationId;
      try
      {
        attributes.SetUserString(ApplicationIdKey, appId);
      }
      catch { }

      // set parameters
      if (parent != null)
      {
        if (StoredObjectParams.ContainsKey(parent.OriginalId))
        {
          var parameters = StoredObjectParams[parent.OriginalId];
          foreach (var member in parameters.GetMembers(DynamicBaseMemberType.Dynamic))
          {
            if (member.Value is Base parameter)
            {
              try
              {
                attributes.SetUserString(member.Key, GetStringFromBaseProp(parameter, "value"));
              }
              catch { }
            }
          }
        }
      }

      // set user dictionaries
      if (obj[UserDictionary] is Base userDictionary)
        ParseDictionaryToArchivable(attributes.UserDictionary, userDictionary);

      var name = obj["name"] as string;
      if (name != null) attributes.Name = name;
    }

    // Clears the stored objects, params, and preview objects
    private void ClearStorage()
    {
      Preview.Clear();
      StoredObjects.Clear();
      StoredObjectParams.Clear();
    }
    #endregion

    #region sending
    public override bool CanPreviewSend => true;
    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      // report and converter
      progress.Report = new ProgressReport();
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      if (converter == null)
      {
        //TODO: Log warning (or throw)
        progress.Report.LogOperationError(new Exception("Could not load converter"));
        return;
      }
      converter.SetContextDocument(Doc);

      // remove any invalid objs
      var filterObjs = GetObjectsFromFilter(state.Filter);
      var existingIds = new List<string>();
      foreach (var id in filterObjs)
      {
        RhinoObject obj = null;
        bool isView = false;
        int viewId = -1;
        RhinoView rhinoView = null;
        try
        {
          obj = Doc.Objects.FindId(new Guid(id)); // this is a rhinoobj
          if (obj == null)
          {
            // Try to check if it's a standard view
            rhinoView = Doc.Views.Find(new Guid(id));
            if (rhinoView != null)
              isView = true;
          }
        }
        catch
        {
          viewId = Doc.NamedViews.FindByName(id);
          if(viewId != -1)
            isView= true;
        }

        if (obj != null)
        {
          var appObj = new ApplicationObject(id, obj.ObjectType.ToString()) { Status = ApplicationObject.State.Unknown };

          if (converter.CanConvertToSpeckle(obj))
            appObj.Update(status: ApplicationObject.State.Created);
          else
            appObj.Update(status: ApplicationObject.State.Failed, logItem: "Object type conversion to Speckle not supported");
          progress.Report.Log(appObj);
          existingIds.Add(id);
        }
        else if (isView)
        {
          var viewObj = viewId != -1 ? new ApplicationObject(id, "Named View") : new ApplicationObject(id, "Standard View");
          if (viewId != -1 || rhinoView != null)
            viewObj.Update(status: ApplicationObject.State.Created);
          else
            viewObj.Update(status: ApplicationObject.State.Failed, logItem: "Does not exist in document");
          progress.Report.Log(viewObj);
        }
        else
        {
          progress.Report.Log(new ApplicationObject(id, "unknown") { Status = ApplicationObject.State.Failed, Log = new List<string>() { "Could not find object in document" } });
          continue;
        }
      }

      if (existingIds.Count == 0)
      {
        //TODO: Log warning (or throw)
        progress.Report.LogOperationError(new Exception("No valid objects selected, nothing will be sent!"));
        return;
      }

      // TODO: instead of selection, consider saving current visibility of objects in doc, hiding everything except selected, and restoring original states on cancel
      Doc.Objects.UnselectAll(false);
      SelectClientObjects(existingIds);
      Doc.Views.Redraw();
    }

    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      converter.SetContextDocument(Doc);

      var streamId = state.StreamId;
      var client = state.Client;

      int objCount = 0;

      state.SelectedObjectIds = GetObjectsFromFilter(state.Filter);
      var commitObject = new Base();

      if (state.SelectedObjectIds.Count == 0)
      {
        throw new InvalidOperationException(
          "Zero objects selected: Please select some objects, or check that your filter can actually select something.");
      }

      progress.Report = new ProgressReport();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = state.SelectedObjectIds.Count;

      foreach (var guid in state.SelectedObjectIds)
      {
        progress.CancellationTokenSource.Token.ThrowIfCancellationRequested();

        Base converted = null;
        string containerName = string.Empty;

        // applicationId can either be doc obj/view guid or name of a NamedView
        RhinoObject obj = null;
        bool isView = false;
        int viewIndex = -1;
        try
        {
          obj = Doc.Objects.FindId(new Guid(guid)); // try get geom object
          if (obj == null)
          {
            // Try to check if it's a standard view
            RhinoView view = Doc.Views.Find(new Guid(guid));
            if (view != null)
              isView = true;
          }
        }
        catch
        {
          viewIndex = Doc.NamedViews.FindByName(guid); // try get a NamedView
          if (viewIndex != -1)
            isView = true;
        }

        string descriptor = string.Empty; ;
        if (isView)
        {
          descriptor = viewIndex != -1 ? "Named View" : "Standard View"; 
        }
        else if(obj != null)
        {
          descriptor = Formatting.ObjectDescriptor(obj);
        }

        var applicationId = obj?.Attributes.GetUserString(ApplicationIdKey) ?? guid;
        ApplicationObject reportObj = new ApplicationObject(guid, descriptor) { applicationId = applicationId };

        if (obj != null)
        {
          if (!converter.CanConvertToSpeckle(obj))
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Sending this object type is not supported in Rhino");
            progress.Report.Log(reportObj);
            continue;
          }

          converter.Report.Log(reportObj); // Log object so converter can access
          converted = converter.ConvertToSpeckle(obj);
          if (converted == null)
          {
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
            progress.Report.Log(reportObj);
            continue;
          }

          if (obj is InstanceObject)
            containerName = "Blocks";
          else
          {
            var layerPath = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
            string cleanLayerPath = RemoveInvalidDynamicPropChars(layerPath);
            containerName = cleanLayerPath;
          }
        }
        else if (isView)
        {
          // Extract the ViewInfo from the NamedViews table or from the Views table
          ViewInfo view = viewIndex != -1 ? Doc.NamedViews[viewIndex] : new ViewInfo(Doc.Views.Find(new Guid(guid)).ActiveViewport);
          converter.Report.Log(reportObj); // Log object so converter can access
          converted = converter.ConvertToSpeckle(view);
          if (converted == null)
          {
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
            progress.Report.Log(reportObj);
            continue;
          }
          containerName = viewIndex != -1 ? "Named Views" : "Standard Views";
        }
        else
        {
          progress.Report.LogOperationError(new Exception($"Failed to find doc object ${guid}."));
          continue;
        }

        if (commitObject[$"@{containerName}"] == null)
          commitObject[$"@{containerName}"] = new List<Base>();
        ((List<Base>)commitObject[$"@{containerName}"]).Add(converted);

        conversionProgressDict["Conversion"]++;
        progress.Update(conversionProgressDict);

        // set application ids, also set for speckle schema base object if it exists
        converted.applicationId = applicationId;
        if (converted["@SpeckleSchema"] != null)
        {
          var newSchemaBase = converted["@SpeckleSchema"] as Base;
          newSchemaBase.applicationId = applicationId;
          converted["@SpeckleSchema"] = newSchemaBase;
        }

        // log report object
        reportObj.Update(status: ApplicationObject.State.Created, logItem: $"Sent as {converted.speckle_type}");
        progress.Report.Log(reportObj);

        objCount++;
      }

      progress.Report.Merge(converter.Report);

      if (objCount == 0)
      {
        throw new InvalidOperationException(
          "Zero objects converted successfully. Send stopped.");
      }

      progress.CancellationTokenSource.Token.ThrowIfCancellationRequested();

      progress.Max = objCount;

      var transports = new List<ITransport>() { new ServerTransport(client.Account, streamId) };

      var objectId = await Operations.Send(
        @object: commitObject,
        cancellationToken: progress.CancellationTokenSource.Token,
        transports: transports,
        onProgressAction: dict =>
        {
          progress.Update(dict);
        },
        onErrorAction: (s, ex) =>
        {
          //TODO: work out if this exception is thrown as expected
          //TODO: Need to ensure transports are disposed properly
          //TODO: Is this the correct type for this exception?
          Log.Warning(ex, "Connector failed during operation {operationName} {errorString}", nameof(Operations.Send), s);
          throw new SpeckleException($"Failed to send objects {s}", ex);
          
          //progress.Report.LogOperationError(e);
          //progress.CancellationTokenSource.Cancel();
        },
        disposeTransports: true
        );

      if (progress.Report.OperationErrorsCount != 0)
        return null;

      progress.CancellationTokenSource.Token.ThrowIfCancellationRequested();

      var actualCommit = new CommitCreateInput
      {
        streamId = streamId,
        objectId = objectId,
        branchName = state.BranchName,
        message = state.CommitMessage != null ? state.CommitMessage : $"Sent {objCount} elements from Rhino.",
        sourceApplication = Utils.RhinoAppName
      };

      if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

      try
      {
        var commitId = await client.CommitCreate(actualCommit);
        state.PreviousCommitId = commitId;
        return commitId;
      }
      catch (Exception ex)
      {
        //TODO is there a better type for this to be
        throw new SpeckleException($"Failed to create commit. reason: {ex.Message}", ex);
      }
    }

    private List<string> GetObjectsFromFilter(ISelectionFilter filter)
    {
      var objs = new List<string>();

      switch (filter.Slug)
      {
        case "manual":
          return filter.Selection;
        case "all":
          objs = Doc.Objects.Where(obj => obj.Visible).Select(obj => obj.Id.ToString()).ToList();
          objs.AddRange(Doc.StandardViews());
          objs.AddRange(Doc.NamedViews());
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
          if (filter.Selection.Contains("Standard Views"))
            objs.AddRange(Doc.StandardViews());
          if (filter.Selection.Contains("Named Views"))
            objs.AddRange(Doc.NamedViews());
          break;
        default:
          //RaiseNotification("Filter type is not supported in this app. Why did the developer implement it in the first place?");
          break;
      }

      return objs;
    }

    private string GetStringFromBaseProp(Base @base, string propName)
    {
      var val = @base[propName];
      if (val == null) return null;
      switch (val)
      {
        case double o:
          return o.ToString();
        case bool o:
          return o.ToString();
        case int o:
          return o.ToString();
        case string o:
          return o;
      }
      return null;
    }
    /// <summary>
    /// Copies a Base to an ArchivableDictionary
    /// </summary>
    /// <param name="target"></param>
    /// <param name="dict"></param>
    private void ParseDictionaryToArchivable(Rhino.Collections.ArchivableDictionary target, Base @base)
    {
      foreach (var prop in @base.GetMemberNames())
      {
        var obj = @base[prop];
        switch (obj)
        {
          case Base o:
            var nested = new Rhino.Collections.ArchivableDictionary();
            ParseDictionaryToArchivable(nested, o);
            target.Set(prop, nested);
            continue;

          case double o:
            target.Set(prop, o);
            continue;

          case bool o:
            target.Set(prop, o);
            continue;

          case int o:
            target.Set(prop, o);
            continue;

          case string o:
            target.Set(prop, o);
            continue;

          case IEnumerable<double> o:
            target.Set(prop, o);
            continue;

          case IEnumerable<bool> o:
            target.Set(prop, o);
            continue;

          case IEnumerable<int> o:
            target.Set(prop, o);
            continue;

          case IEnumerable<string> o:
            target.Set(prop, o);
            continue;

          default:
            continue;
        }
      }
    }

    private string RemoveInvalidDynamicPropChars(string str)
    {
      // remove ./
      return Regex.Replace(str, @"[./]", "-");
    }

    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    #endregion

    public override bool CanOpen3DView => true;

    public override async Task Open3DView(List<double> viewCoordinates, string viewName = "")
    {
      // Create positional objects for camera
      Point3d cameraLocation = new Point3d(viewCoordinates[0], viewCoordinates[1], viewCoordinates[2]);
      Point3d target = new Point3d(viewCoordinates[3], viewCoordinates[4], viewCoordinates[5]);
      Vector3d direction = target - cameraLocation;

      if (!Doc.Views.Any(v => v.ActiveViewport.Name == "SpeckleCommentView"))
      {
        // Get bounds from active view
        Rectangle bounds = Doc.Views.ActiveView.ScreenRectangle;
        // Reset margins
        bounds.X = 0;
        bounds.Y = 0;
        Doc.Views.Add("SpeckleCommentView", DefinedViewportProjection.Perspective, bounds, false);
      }

      await Task.Run(() =>
      {
        IEnumerable<RhinoView> views = Doc.Views.Where(v => v.ActiveViewport.Name == "SpeckleCommentView");
        if (views.Any())
        {
          RhinoView speckleCommentView = views.First();
          speckleCommentView.ActiveViewport.SetCameraDirection(direction, false);
          speckleCommentView.ActiveViewport.SetCameraLocation(cameraLocation, true);

          DisplayModeDescription shaded = DisplayModeDescription.FindByName("Shaded");
          if (shaded != null)
          {
            speckleCommentView.ActiveViewport.DisplayMode = shaded;
          }

          // Minimized all maximized views.
          IEnumerable<RhinoView> maximizedViews = Doc.Views.Where(v => v.Maximized);
          foreach (RhinoView view in maximizedViews)
          {
            view.Maximized = false;
          }

          // Maximized speckle comment view.
          speckleCommentView.Maximized = true;

          if (Doc.Views.ActiveView.ActiveViewport.Name != "SpeckleCommentView")
          {
            Doc.Views.ActiveView = speckleCommentView;
          }
        }

        Doc.Views.Redraw();
      });
    }
  }
}
