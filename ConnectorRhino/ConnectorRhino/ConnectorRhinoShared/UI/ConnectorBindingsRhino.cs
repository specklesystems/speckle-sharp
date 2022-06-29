using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;

using ApplicationObject = Speckle.Core.Models.ApplicationObject;

namespace SpeckleRhino
{
  public partial class ConnectorBindingsRhino : ConnectorBindings
  {
    public RhinoDoc Doc { get => RhinoDoc.ActiveDoc; }

    private static string SpeckleKey = "speckle2";
    private static string UserStrings = "userStrings";
    private static string UserDictionary = "userDictionary";

    public ISpeckleConverter Converter { get; set; } = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
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
      return new List<ReceiveMode> { ReceiveMode.Create };
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
      //converter.Report.LogOperationError();
      RhinoApp.WriteLine("Deselected unsupported objects:");
      foreach (var entry in reportLog)
        Rhino.RhinoApp.WriteLine($"{entry.Value} of type {entry.Key}");
    }
    public override List<string> GetSelectedObjects()
    {
      var objs = new List<string>();
      if (Doc == null) return objs;

      Converter.SetContextDocument(Doc);
      
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

    public override void SelectClientObjects(List<string> objs)
    {
      foreach (var id in objs)
      {
        RhinoObject obj = Doc.Objects.FindId(new Guid(id));
        if (obj != null)
        {
          obj.Select(true, true, false, false, true, true);
        }
      }
    }

    public override void ResetDocument()
    {
      if (PreviewConduit != null)
        PreviewConduit.Enabled = false;

      Doc.Objects.UnselectAll(false); // TODO: consider instead of unselecting, storing doc visibility state and restoring to this point
      Doc.Views.Redraw();
    }

    #endregion

    #region receiving 
    public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
    {
      // first check if commit is the same and preview objects have already been generated
      Commit commit = await GetCommitFromState(state, progress);

      if (commit.id != SelectedReceiveCommit)
      {
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
        Preview = FlattenCommitObject(commitObject, commitLayerName, progress, ref count);
        Doc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info
        Preview.ForEach(o => o.Converted = o.Convertible ?
          ConvertObject(o, progress) :
          o.Fallback.SelectMany(f => ConvertObject(f, progress)).ToList());
        // undo notes edit
        var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
        Doc.Notes = segments[0];
      }

      // create display conduit
      PreviewConduit = new PreviewConduit();
      PreviewConduit.Preview = Preview;
      PreviewConduit.Enabled = true;
      Doc.Views.Redraw();

      if (progress.CancellationTokenSource.Token.IsCancellationRequested)
      {
        PreviewConduit.Enabled = false;
        return null;
      }

      return state;
    }

    public override async Task<StreamState> ReceiveStream(StreamState state, ProgressViewModel progress)
    {
      var kit = KitManager.GetDefaultKit();
      if (Converter == null)
        throw new Exception("Could not find any Kit!");
      Converter.SetContextDocument(Doc);

      Commit commit = await GetCommitFromState(state, progress);
      if (commit == null) return null;
      if (SelectedReceiveCommit != commit.id)
      {
        Preview.Clear();
        StoredObjects.Clear();
        SelectedReceiveCommit = commit.id;
      }

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
          Preview = FlattenCommitObject(commitObject, commitLayerName, progress, ref count);

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            return;
        }

        if (progress.Report.OperationErrorsCount != 0)
          return;

        // bake preview objects
        foreach (var previewObj in Preview)
        {
          if (previewObj.Converted == null || previewObj.Converted.Count == 0)
          {
            if (previewObj.Convertible)
              previewObj.Converted = ConvertObject(previewObj, progress);
            else
              previewObj.Converted = previewObj.Fallback.SelectMany(o => ConvertObject(o, progress)).ToList();
          }

          if (previewObj.Converted != null)
            BakeObject(previewObj, progress);

          if (progress.CancellationTokenSource.Token.IsCancellationRequested)
            return;
          conversionProgressDict["Conversion"]++;
          progress.Update(conversionProgressDict);
        }

        // undo notes edit
        var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
        Doc.Notes = segments[0];
      });

      progress.Report.Merge(Converter.Report);
      Doc.Views.Redraw();
      Doc.EndUndoRecord(undoRecord);

      return state;
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
    private List<ApplicationObject> FlattenCommitObject(object obj, string layer, ProgressViewModel progress, ref int count, bool foundConvertibleMember = false)
    {
      var objects = new List<ApplicationObject>();

      if (obj is Base @base)
      {
        var appObj = new ApplicationObject(@base.id) { Container = layer };
        if (Converter.CanConvertToNative(@base))
        {
          appObj.Convertible = true;
          objects.Add(appObj);
          StoredObjects.Add(@base.id, @base);
          return objects;
        }
        else
        {
          appObj.Convertible = false;

          // handle fallback display separately
          // NOTE: deprecated displayMesh!!
          bool hasFallback = false;
          if (@base.GetMembers().ContainsKey("displayValue"))
          {
            var fallbackObjects = FlattenCommitObject(@base["displayValue"], layer, progress, ref count, foundConvertibleMember);
            if (fallbackObjects.Count > 0)
            {
              appObj.Fallback.AddRange(fallbackObjects);
              foundConvertibleMember = true;
              hasFallback = true;
            }
          }
          if (hasFallback)
            objects.Add(appObj);

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

            var nestedObjects = FlattenCommitObject(@base[prop], rhLayerName, progress, ref count, foundConvertibleMember);
            if (nestedObjects.Count > 0)
            {
              objects.AddRange(nestedObjects);
              foundConvertibleMember = true;
            }
          }

          if (!foundConvertibleMember && count == totalMembers) // this was an unsupported geo
          {
            appObj.Update(status: ApplicationObject.ConversionStatus.Skipped, logItem: $"Receiving objects of type {@base.speckle_type} not supported in Rhino");
            progress.Report.Log(appObj);
          }

          return objects;
        }
      }

      if (obj is IReadOnlyList<object> list)
      {
        count = 0;
        foreach (var listObj in list)
          objects.AddRange(FlattenCommitObject(listObj, layer, progress, ref count));
        return objects;
      }

      if (obj is IDictionary dict)
      {
        count = 0;
        foreach (DictionaryEntry kvp in dict)
          objects.AddRange(FlattenCommitObject(kvp.Value, layer, progress, ref count));
        return objects;
      }

      return objects;
    }

    // conversion and bake
    private List<object> ConvertObject(ApplicationObject previewObj, ProgressViewModel progress)
    {
      var obj = StoredObjects[previewObj.id];
      var convertedList = new List<object>();

      // add preview object report object to progress for converter
      previewObj.Status = ApplicationObject.ConversionStatus.Converting;
      progress.Report.Log(previewObj);

      var converted = Converter.ConvertToNative(obj);
      if (converted == null)
      {
        previewObj.Update(status: ApplicationObject.ConversionStatus.Failed, logItem: $"Conversion of {obj.speckle_type} returned Null");
        return convertedList;
      }

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
    private void BakeObject(ApplicationObject previewObj, ProgressViewModel progress)
    {
      var obj = StoredObjects[previewObj.id];
      string layerPath = previewObj.Container;
      var converted = previewObj.Converted;
      int bakedCount = 0;
      foreach (var convertedItem in converted)
      {
        if (!(convertedItem is GeometryBase convertedRH)) continue;

        if (!convertedRH.IsValidWithLog(out string log))
        {
          previewObj.Update(log: new List<string>() { $"{log.Replace("\n", "").Replace("\r", "")}" });
          continue;
        }

        Layer bakeLayer = Doc.GetLayer(layerPath, true);
        if (bakeLayer == null)
        {
          previewObj.Update(log: new List<string>() { $"Could not create layer {layerPath}." });
          continue;
        }

        var attributes = new ObjectAttributes();

        // handle display style
        if (obj[@"displayStyle"] is Base display)
          if (Converter.ConvertToNative(display) is ObjectAttributes displayAttribute)
            attributes = displayAttribute;
        else if (obj[@"renderMaterial"] is Base renderMaterial)
          attributes.ColorSource = ObjectColorSource.ColorFromMaterial;

        // assign layer
        attributes.LayerIndex = bakeLayer.Index;

        // handle user strings
        if (obj[UserStrings] is Dictionary<string, object> userStrings)
          foreach (var key in userStrings.Keys)
            attributes.SetUserString(key, userStrings[key] as string);

        // handle user dictionaries
        if (obj[UserDictionary] is Dictionary<string, object> dict)
          ParseDictionaryToArchivable(attributes.UserDictionary, dict);

        Guid id = Doc.Objects.Add(convertedRH, attributes);
        if (id == Guid.Empty)
        {
          previewObj.Update(log: new List<string>() { $"Could not add to document." });
          continue;
        }
        previewObj.Update(createdId: id.ToString());

        // handle render material
        if (obj[@"renderMaterial"] is Base render)
        {
          var convertedMaterial = Converter.ConvertToNative(render); //Maybe wrap in try catch in case no conversion exists?
          if (convertedMaterial is RenderMaterial rm)
          {
            var rhinoObject = Doc.Objects.FindId(id);
            rhinoObject.RenderMaterial = rm;
            rhinoObject.CommitChanges();
          }
        }
      }
      bakedCount++;

      if (bakedCount == 0)
        previewObj.Update(status: ApplicationObject.ConversionStatus.Failed, logItem: $"Could not create object {obj.id} of type {obj.speckle_type}");
      else
        previewObj.Update(status: ApplicationObject.ConversionStatus.Created);
      progress.Report.Log(previewObj);
    }

    #endregion

    #region sending
    public override async void PreviewSend(StreamState state, ProgressViewModel progress)
    {
      // TODO: instead of selection, consider saving current visibility of objects in doc, hiding everything except selected, and restoring original states on cancel
      Doc.Objects.UnselectAll(false);
      SelectClientObjects(GetObjectsFromFilter(state.Filter));
      Doc.Views.Redraw();
    }
    public override async Task<string> SendStream(StreamState state, ProgressViewModel progress)
    {
      Converter.SetContextDocument(Doc);

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

      var conversionProgressDict = new ConcurrentDictionary<string, int>();
      conversionProgressDict["Conversion"] = 0;

      progress.Max = state.SelectedObjectIds.Count;

      foreach (var applicationId in state.SelectedObjectIds)
      {
        if (progress.CancellationTokenSource.Token.IsCancellationRequested)
          return null;

        Base converted = null;
        ApplicationObject reportObj = new ApplicationObject(applicationId);
        string containerName = string.Empty;

        // applicationId can either be doc obj guid or name of view
        RhinoObject obj = null;
        int viewIndex = -1;
        try
        {
          obj = Doc.Objects.FindId(new Guid(applicationId)); // try get geom object
        }
        catch
        {
          viewIndex = Doc.NamedViews.FindByName(applicationId); // try get view
        }

        if (obj != null)
        {
          if (!Converter.CanConvertToSpeckle(obj))
          {
            reportObj.Update(status: ApplicationObject.ConversionStatus.Skipped, logItem: $"Sending objects of type {obj.ObjectType} not supported in Rhino");
            progress.Report.Log(reportObj);
            continue;
          }
          reportObj.Update(status: ApplicationObject.ConversionStatus.Converting);

          Converter.Report.Log(reportObj); // Log object so converter can access
          converted = Converter.ConvertToSpeckle(obj);
          if (converted == null)
          {
            reportObj.Update(status: ApplicationObject.ConversionStatus.Failed, logItem: $"Conversion of {obj.ObjectType} returned Null");
            progress.Report.Log(reportObj);
            continue;
          }

          // attach user strings, dictionaries, and name
          var userStrings = obj.Attributes.GetUserStrings();
          var userStringDict = userStrings.AllKeys.ToDictionary(k => k, k => userStrings[k]);
          converted[UserStrings] = userStringDict;

          var userDict = new Dictionary<string, object>();
          ParseArchivableToDictionary(userDict, obj.Attributes.UserDictionary);
          converted[UserDictionary] = userDict;

          if (obj.HasName) converted["name"] = obj.Name;

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
          reportObj.Update(status: ApplicationObject.ConversionStatus.Converting);
          Converter.Report.Log(reportObj); // Log object so converter can access
          converted = Converter.ConvertToSpeckle(view);
          if (converted == null)
          {
            reportObj.Update(status: ApplicationObject.ConversionStatus.Failed, logItem: $"Creation of {view.GetType()} returned Null");
            progress.Report.Log(reportObj);
            continue;
          }
          containerName = "Named Views";
        }
        else
        {
          progress.Report.LogOperationError(new Exception($"Failed to find doc object ${applicationId}."));
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
        reportObj.Update(status: ApplicationObject.ConversionStatus.Created, logItem: $"Successfully converted Rhino {obj.ObjectType} to Speckle {converted.speckle_type}");
        progress.Report.Log(reportObj);

        objCount++;
      }

      progress.Report.Merge(Converter.Report);

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

    /// <summary>
    /// Copies an ArchivableDictionary to a Dictionary
    /// </summary>
    /// <param name="target"></param>
    /// <param name="dict"></param>
    private void ParseArchivableToDictionary(Dictionary<string, object> target, Rhino.Collections.ArchivableDictionary dict)
    {
      foreach (var key in dict.Keys)
      {
        var obj = dict[key];
        switch (obj)
        {
          case Rhino.Collections.ArchivableDictionary o:
            var nested = new Dictionary<string, object>();
            ParseArchivableToDictionary(nested, o);
            target[key] = nested;
            continue;

          case double _:
          case bool _:
          case int _:
          case string _:
          case IEnumerable<double> _:
          case IEnumerable<bool> _:
          case IEnumerable<int> _:
          case IEnumerable<string> _:
            target[key] = obj;
            continue;

          default:
            continue;
        }
      }
    }

    /// <summary>
    /// Copies a Dictionary to an ArchivableDictionary
    /// </summary>
    /// <param name="target"></param>
    /// <param name="dict"></param>
    private void ParseDictionaryToArchivable(Rhino.Collections.ArchivableDictionary target, Dictionary<string, object> dict)
    {
      foreach (var key in dict.Keys)
      {
        var obj = dict[key];
        switch (obj)
        {
          case Dictionary<string, object> o:
            var nested = new Rhino.Collections.ArchivableDictionary();
            ParseDictionaryToArchivable(nested, o);
            target.Set(key, nested);
            continue;

          case double o:
            target.Set(key, o);
            continue;

          case bool o:
            target.Set(key, o);
            continue;

          case int o:
            target.Set(key, o);
            continue;

          case string o:
            target.Set(key, o);
            continue;

          case IEnumerable<double> o:
            target.Set(key, o);
            continue;

          case IEnumerable<bool> o:
            target.Set(key, o);
            continue;

          case IEnumerable<int> o:
            target.Set(key, o);
            continue;

          case IEnumerable<string> o:
            target.Set(key, o);
            continue;

          default:
            continue;
        }
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
