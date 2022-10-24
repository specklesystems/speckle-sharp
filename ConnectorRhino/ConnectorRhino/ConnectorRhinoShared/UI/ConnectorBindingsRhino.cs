using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
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
      var projectInfo = new List<string> { "Named Views" };

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
        Doc.Objects.UnselectAll(true);

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
    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      // first check if commit is the same and preview objects have already been generated
      Commit commit = await GetCommitFromState(state, progress);
      progress.Report = new ProgressReport();

      if (commit.id != SelectedReceiveCommit)
      {
        // check for converter 
        var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
        if (converter == null)
        {
          progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
          return null;
        }
        converter.SetContextDocument(Doc);

        var commitObject = await GetCommit(commit, state, progress);
        if (commitObject == null)
        {
          progress.Report.LogOperationError(new Exception($"Could not retrieve commit {commit.id} from server"));
          progress.CancellationTokenSource.Cancel();
        }

        SelectedReceiveCommit = commit.id;
        Preview.Clear();
        StoredObjects.Clear();

        int count = 0;
        var commitLayerName = DesktopUI2.Formatting.CommitInfo(state.CachedStream.name, state.BranchName, commit.id); // get commit layer name 
        Preview = FlattenCommitObject(commitObject, converter, progress, commitLayerName, ref count);
        Doc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info

        // Convert preview objects
        foreach (var previewObj in Preview)
        {
          previewObj.CreatedIds = new List<string>() { previewObj.OriginalId }; // temporary store speckle id as created id for Preview report selection to work

          var storedObj = StoredObjects[previewObj.OriginalId];
          if (storedObj.speckle_type.Contains("Block") || storedObj.speckle_type.Contains("View"))
          {
            var status = previewObj.Convertible ? ApplicationObject.State.Created : ApplicationObject.State.Skipped;
            previewObj.Update(status: status, logItem: "No preview available");
            progress.Report.Log(previewObj);
            continue;
          }

          if (previewObj.Convertible)
            previewObj.Converted = ConvertObject(previewObj, converter);
          else
            foreach (var fallback in previewObj.Fallback)
            {
              fallback.Converted = ConvertObject(fallback, converter);
              previewObj.Log.AddRange(fallback.Log);
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

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      // check for converter 
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      if (converter == null)
      {
        progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
        return null;
      }
      converter.SetContextDocument(Doc);
      converter.ReceiveMode = state.ReceiveMode;

      Commit commit = await GetCommitFromState(state, progress);
      if (commit == null) return null;

      state.LastSourceApp = commit.sourceApplication;

      if (SelectedReceiveCommit != commit.id)
      {
        Preview.Clear();
        StoredObjects.Clear();
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
      if (progress.Report.OperationErrorsCount != 0)
        return null;



      RhinoApp.InvokeOnUiThread((Action)delegate
      {
        RhinoDoc.ActiveDoc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info

        // create preview objects if they don't already exist
        if (Preview.Count == 0)
        {
          // flatten the commit object to retrieve children objs
          int count = 0;
          Preview = FlattenCommitObject(commitObject, converter, progress, commitLayerName, ref count);

          // convert
          foreach (var previewObj in Preview)
          {
            if (previewObj.Convertible)
              previewObj.Converted = ConvertObject(previewObj, converter);
            else
              foreach (var fallback in previewObj.Fallback)
              {
                fallback.Converted = ConvertObject(fallback, converter);
                previewObj.Log.AddRange(fallback.Log);
              }

            if (previewObj.Converted == null || previewObj.Converted.Count == 0)
            {
              var convertedFallback = previewObj.Fallback.Where(o => o.Converted != null || o.Converted.Count > 0);
              if (convertedFallback != null && convertedFallback.Count() > 0)
                previewObj.Update(logItem: $"Creating with {convertedFallback.Count()} fallback values");
              else
                previewObj.Update(status: ApplicationObject.State.Failed, logItem: $"Couldn't convert object or any fallback values");
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
          switch (state.ReceiveMode)
          {
            case ReceiveMode.Update: // existing objs will be removed if it exists in the received commit
              toRemove = GetObjectsByApplicationId(previewObj.applicationId);
              toRemove.ForEach(o => Doc.Objects.Delete(o));
              break;
            default:
              break;
          }
          if (toRemove.Count() > 0) isUpdate = true;

          // bake

          previewObj.CreatedIds.Clear(); // clear created ids before bake because these may be speckle ids from the preview

          if (previewObj.Convertible)
            BakeObject(previewObj, converter);
          else
          {
            foreach (var fallback in previewObj.Fallback)
              BakeObject(fallback, converter, previewObj);
            previewObj.Status = previewObj.Fallback.Where(o => o.Status == ApplicationObject.State.Failed).Count() == previewObj.Fallback.Count ?
              ApplicationObject.State.Failed : isUpdate ?
              ApplicationObject.State.Updated : ApplicationObject.State.Created;
          }

          progress.Report.Log(previewObj);

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            return;
          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);
        }

        // undo notes edit
        var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
        Doc.Notes = segments[0];
      });

      Doc.Views.Redraw();
      Doc.EndUndoRecord(undoRecord);

      return state;
    }

    // gets objects by id directly or by applicaiton id user string
    private List<RhinoObject> GetObjectsByApplicationId(string applicationId)
    {
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
    private async Task<Commit> GetCommitFromState(StreamState state, ProgressViewModel progress)
    {
      Commit commit = null;
      if (state.CommitId == "latest") //if "latest", always make sure we get the latest commit
      {
        var res = await state.Client.BranchGet(progress.CancellationTokenSource.Token, state.StreamId, state.BranchName, 1);
        commit = res.commits.items.FirstOrDefault();
      }
      else
      {
        var res = await state.Client.CommitGet(progress.CancellationTokenSource.Token, state.StreamId, state.CommitId);
        commit = res;
      }
      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;
      return commit;
    }
    private async Task<Base> GetCommit(Commit commit, StreamState state, ProgressViewModel progress)
    {
      var transport = new ServerTransport(state.Client.Account, state.StreamId);

      var commitObject = await Operations.Receive(
        commit.referencedObject,
        progress.CancellationTokenSource.Token,
        transport,
        onProgressAction: dict => progress.Update(dict),
        onErrorAction: (s, e) =>
        {
          progress.Report.LogOperationError(e);
          progress.CancellationTokenSource.Cancel();
        },
        onTotalChildrenCountKnown: (c) => progress.Max = c,
        disposeTransports: true
        );

      if (progress.Report.OperationErrorsCount != 0)
        return null;

      return commitObject;
    }

    // Recurses through the commit object and flattens it. Returns list of Preview objects
    private List<ApplicationObject> FlattenCommitObject(object obj, ISpeckleConverter converter, ProgressViewModel progress, string layer, ref int count, bool foundConvertibleMember = false)
    {
      var objects = new List<ApplicationObject>();

      if (obj is Base @base)
      {
        var speckleType = @base.speckle_type.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        var appObj = new ApplicationObject(@base.id, speckleType) { applicationId = @base.applicationId, Container = layer };
        if (converter.CanConvertToNative(@base))
        {
          appObj.Convertible = true;
          if (StoredObjects.ContainsKey(@base.id))
            appObj.Update(logItem: $"Found another {speckleType} in this commit with the same id. Skipped other object");
          else
            StoredObjects.Add(@base.id, @base);
          objects.Add(appObj);
          return objects;
        }
        else
        {
          appObj.Convertible = false;

          // handle fallback display separately
          bool hasFallback = false;
          if (@base.GetMembers().ContainsKey("displayValue"))
          {
            var fallbackObjects = FlattenCommitObject(@base["displayValue"], converter, progress, layer, ref count, foundConvertibleMember);
            if (fallbackObjects.Count > 0)
            {
              appObj.Fallback.AddRange(fallbackObjects);
              foundConvertibleMember = true;
              hasFallback = true;
            }
          }
          if (hasFallback)
          {
            if (StoredObjects.ContainsKey(@base.id))
              appObj.Update(logItem: $"Found another {speckleType} in this commit with the same id. Skipped other object");
            else
              StoredObjects.Add(@base.id, @base);
            objects.Add(appObj);
          }

          // handle any children elements, these are added as separate previewObjects
          List<string> props = @base.GetDynamicMembers().ToList();
          if (@base.GetMembers().ContainsKey("elements")) // this is for builtelements like roofs, walls, and floors.
            props.Add("elements");
          int totalMembers = props.Count;
          foreach (var prop in props)
          {
            count++;

            // get bake layer name
            string objLayerName = prop.StartsWith("@") ? prop.Remove(0, 1) : prop;
            string rhLayerName = objLayerName.StartsWith($"{layer}{Layer.PathSeparator}") ? objLayerName : $"{layer}{Layer.PathSeparator}{objLayerName}";

            var nestedObjects = FlattenCommitObject(@base[prop], converter, progress, rhLayerName, ref count, foundConvertibleMember);
            var validNestedObjects = nestedObjects.Where(o => o.Convertible == true || o.Fallback.Count > 0)?.ToList();
            if (validNestedObjects != null && validNestedObjects.Count > 0)
            {
              objects.AddRange(nestedObjects);
              foundConvertibleMember = true;
            }
          }

          if (!foundConvertibleMember && count == totalMembers) // this was an unsupported geo
          {
            appObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Receiving this object type is not supported in Rhino");
            objects.Add(appObj);
          }

          return objects;
        }
      }

      if (obj is IReadOnlyList<object> list)
      {
        count = 0;
        foreach (var listObj in list)
          objects.AddRange(FlattenCommitObject(listObj, converter, progress, layer, ref count));
        return objects;
      }

      if (obj is IDictionary dict)
      {
        count = 0;
        foreach (DictionaryEntry kvp in dict)
          objects.AddRange(FlattenCommitObject(kvp.Value, converter, progress, layer, ref count));
        return objects;
      }

      return objects;
    }

    // conversion and bake
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
    private void BakeObject(ApplicationObject appObj, ISpeckleConverter converter, ApplicationObject parent = null)
    {
      var obj = StoredObjects[appObj.OriginalId];
      int bakedCount = 0;
      // check if this is a view or block - convert instead of bake if so (since these are "baked" during conversion)
      if (!appObj.Converted.Any() && (obj.speckle_type.Contains("Block") || obj.speckle_type.Contains("View")))
      {
        appObj.Converted = ConvertObject(appObj, converter);
      }
      foreach (var convertedItem in appObj.Converted)
      {
        switch (convertedItem)
        {
          case GeometryBase o:
            string layerPath = appObj.Container;
            if (!o.IsValidWithLog(out string log))
            {
              var invalidMessage = $"{log.Replace("\n", "").Replace("\r", "")}";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.id}: {invalidMessage}");
              else
                appObj.Update(logItem: invalidMessage);
              continue;
            }
            Layer bakeLayer = Doc.GetLayer(layerPath, true);
            if (bakeLayer == null)
            {
              var layerMessage = $"Could not create layer {layerPath}.";
              if (parent != null)
                parent.Update(logItem: $"fallback {appObj.id}: {layerMessage}");
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
                parent.Update(logItem: $"fallback {appObj.id}: {bakeMessage}");
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
              appObj.Update(status: ApplicationObject.State.Created, createdId: o.Id.ToString());
            bakedCount++;
            break;
          case string o: // this was prbly a view, baked during conversion
            if (parent != null)
              parent.Update(createdId: o);
            else
              appObj.Update(status: ApplicationObject.State.Created, createdId: o);
            bakedCount++;
            break;
          default:
            break;
        }
      }

      if (bakedCount == 0)
      {
        if (parent != null)
          parent.Update(logItem: $"fallback {appObj.id}: could not bake object");
        else
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not bake object");
      }
      else
        appObj.Update(status: ApplicationObject.State.Created);
    }

    private void SetUserInfo(Base obj, ObjectAttributes attributes, ApplicationObject parent = null)
    {
      if (obj[UserStrings] is Base userStrings)
        foreach (var key in userStrings.GetMemberNames())
          attributes.SetUserString(key, userStrings[key] as string);

      // set application id
      var appId = parent != null ? parent.applicationId : obj.applicationId;
      try
      {
        attributes.SetUserString(ApplicationIdKey, appId);
      }
      catch { }

      if (obj[UserDictionary] is Base userDictionary)
        ParseDictionaryToArchivable(attributes.UserDictionary, userDictionary);

      var name = obj["name"] as string;
      if (name != null) attributes.Name = name;
    }

    #endregion

    #region sending
    public override bool CanPreviewSend => true;
    public override void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      progress.Report = new ProgressReport();

      var filterObjs = GetObjectsFromFilter(state.Filter);

      // remove any invalid objs
      var existingIds = new List<string>();
      foreach (var id in filterObjs)
      {
        RhinoObject obj = null;
        try
        {
          obj = Doc.Objects.FindId(new Guid(id)); // this is a rhinoobj
        }
        catch
        {
          var viewId = Doc.NamedViews.FindByName(id);
          var viewObj = new ApplicationObject(id, "Named View");
          if (viewId != -1)
            viewObj.Update(status: ApplicationObject.State.Created);
          else
            viewObj.Update(status: ApplicationObject.State.Failed, logItem: "Does not exist in document");
          progress.Report.Log(viewObj);
          continue;
        }

        if (obj == null)
        {
          progress.Report.Log(new ApplicationObject(id, "unknown") { Status = ApplicationObject.State.Failed, Log = new List<string>() { "Could not find object in document" } });
          continue;
        }

        // get converter
        var appObj = new ApplicationObject(id, obj.ObjectType.ToString()) { Status = ApplicationObject.State.Unknown };
        var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
        if (converter != null)
        {
          converter.SetContextDocument(Doc);
          if (converter.CanConvertToSpeckle(obj))
            appObj.Update(status: ApplicationObject.State.Created);
          else
            appObj.Update(status: ApplicationObject.State.Failed, logItem: "Object type conversion to Speckle not supported");
        }
        else
          appObj.Update(logItem: "Converter not found, conversion status could not be determined");
        existingIds.Add(id);
      }

      if (existingIds.Count == 0)
      {
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
      // check for converter 
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      if (converter == null)
      {
        progress.Report.LogOperationError(new SpeckleException("Could not find any Kit!"));
        return null;
      }
      converter.SetContextDocument(Doc);

      var streamId = state.StreamId;
      var client = state.Client;

      int objCount = 0;

      state.SelectedObjectIds = GetObjectsFromFilter(state.Filter);
      var commitObject = new Base();

      if (state.SelectedObjectIds.Count == 0)
      {
        progress.Report.LogOperationError(new SpeckleException("Zero objects selected; send stopped. Please select some objects, or check that your filter can actually select something.", false));
        return null;
      }

      progress.Report = new ProgressReport();
      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = state.SelectedObjectIds.Count;

      foreach (var guid in state.SelectedObjectIds)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
          return null;

        Base converted = null;
        string containerName = string.Empty;

        // applicationId can either be doc obj guid or name of view
        RhinoObject obj = null;
        int viewIndex = -1;
        try
        {
          obj = Doc.Objects.FindId(new Guid(guid)); // try get geom object
        }
        catch
        {
          viewIndex = Doc.NamedViews.FindByName(guid); // try get view
        }
        var descriptor = obj != null ? Formatting.ObjectDescriptor(obj) : "Named View";
        var applicationId = obj.Attributes.GetUserString(ApplicationIdKey) ?? guid;
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
        else if (viewIndex != -1)
        {
          ViewInfo view = Doc.NamedViews[viewIndex];
          converter.Report.Log(reportObj); // Log object so converter can access
          converted = converter.ConvertToSpeckle(view);
          if (converted == null)
          {
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
            progress.Report.Log(reportObj);
            continue;
          }
          containerName = "Named Views";
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
        progress.Report.LogOperationError(new SpeckleException("Zero objects converted successfully. Send stopped.", false));
        return null;
      }

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

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
        onErrorAction: (s, e) =>
        {
          progress.Report.LogOperationError(e);
          progress.CancellationTokenSource.Cancel();
        },
        disposeTransports: true
        );

      if (progress.Report.OperationErrorsCount != 0)
        return null;

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
        return null;

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
      catch (Exception e)
      {
        progress.Report.LogOperationError(e);
      }
      return null;

      //return state;
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
          //RaiseNotification("Filter type is not supported in this app. Why did the developer implement it in the first place?");
          break;
      }

      return objs;
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

  }
}
