using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Models.Settings;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.Collections;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Render;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using static DesktopUI2.ViewModels.MappingViewModel;
using Utilities = Speckle.Core.Models.Utilities;

namespace SpeckleRhino;

public class ConnectorBindingsRhino : ConnectorBindings
{
  private static string SpeckleKey = "speckle2";
  private static string UserStrings = "userStrings";
  private static string UserDictionary = "userDictionary";
  private static string ApplicationIdKey = "applicationId";
  public Dictionary<string, Base> StoredObjectParams = new(); // these are to store any parameters found on parent objects to add to fallback objects

  public Dictionary<string, Base> StoredObjects = new();

  public ConnectorBindingsRhino()
  {
    RhinoDoc.EndOpenDocument += RhinoDoc_EndOpenDocument;
    RhinoDoc.LayerTableEvent += RhinoDoc_LayerChange; // used to update the DUI2 layer filter to reflect layer changes
  }

  public RhinoDoc Doc => RhinoDoc.ActiveDoc;
  public List<ApplicationObject> Preview { get; set; } = new();
  public PreviewConduit PreviewConduit { get; set; }
  private string SelectedReceiveCommit { get; set; }

  public override bool CanOpen3DView => true;

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

  private void RhinoDoc_LayerChange(object sender, LayerTableEventArgs e)
  {
    if (UpdateSelectedStream != null)
      UpdateSelectedStream();
  }

  public override List<ReceiveMode> GetReceiveModes()
  {
    return new List<ReceiveMode> { ReceiveMode.Update, ReceiveMode.Create };
  }

  public override async Task Open3DView(List<double> viewCoordinates, string viewName = "")
  {
    // Create positional objects for camera
    Point3d cameraLocation = new(viewCoordinates[0], viewCoordinates[1], viewCoordinates[2]);
    Point3d target = new(viewCoordinates[3], viewCoordinates[4], viewCoordinates[5]);
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
          speckleCommentView.ActiveViewport.DisplayMode = shaded;

        // Minimized all maximized views.
        IEnumerable<RhinoView> maximizedViews = Doc.Views.Where(v => v.Maximized);
        foreach (RhinoView view in maximizedViews)
          view.Maximized = false;

        // Maximized speckle comment view.
        speckleCommentView.Maximized = true;

        if (Doc.Views.ActiveView.ActiveViewport.Name != "SpeckleCommentView")
          Doc.Views.ActiveView = speckleCommentView;
      }

      Doc.Views.Redraw();
    });
  }

  #region Local streams I/O with local file

  public override List<StreamState> GetStreamsInFile()
  {
    var strings = Doc?.Strings.GetEntryNames(SpeckleKey);

    if (strings == null)
      return new List<StreamState>();

    var states = strings
      .Select(s => JsonConvert.DeserializeObject<StreamState>(Doc.Strings.GetValue(SpeckleKey, s)))
      .ToList();
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

  public override string GetHostAppNameVersion()
  {
    return Utils.RhinoAppName;
  }

  public override string GetHostAppName()
  {
    return HostApplications.Rhino.Slug;
  }

  public override string GetDocumentId()
  {
    return Utilities.hashString("X" + Doc?.Path + Doc?.Name, Utilities.HashingFuctions.MD5);
  }

  public override string GetDocumentLocation()
  {
    return Doc?.Path;
  }

  public override string GetFileName()
  {
    return Doc?.Name;
  }

  //improve this to add to log??
  private void LogUnsupportedObjects(List<RhinoObject> objs, ISpeckleConverter converter)
  {
    var reportLog = new Dictionary<string, int>();
    foreach (var obj in objs)
    {
      var type = obj.ObjectType.ToString();
      if (reportLog.ContainsKey(type))
        reportLog[type] = reportLog[type]++;
      else
        reportLog.Add(type, 1);
    }
    RhinoApp.WriteLine("Deselected unsupported objects:");
    foreach (var entry in reportLog)
      RhinoApp.WriteLine($"{entry.Value} of type {entry.Key}");
  }

  public override List<string> GetSelectedObjects()
  {
    var objs = new List<string>();
    var Converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);

    if (Converter == null || Doc == null)
      return objs;

    var selected = Doc.Objects.GetSelectedObjects(true, false).ToList();
    if (selected.Count == 0)
      return objs;
    var supportedObjs = selected.Where(o => Converter.CanConvertToSpeckle(o))?.ToList();
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
    var projectInfo = new List<string> { "Named Views", "Standard Views", "Layers" };

    return new List<ISelectionFilter>
    {
      new AllSelectionFilter
      {
        Slug = "all",
        Name = "Everything",
        Icon = "CubeScan",
        Description = "Selects all document objects and project info."
      },
      new ListSelectionFilter
      {
        Slug = "layer",
        Name = "Layers",
        Icon = "LayersTriple",
        Description = "Selects objects based on their layers.",
        Values = layers
      },
      new ListSelectionFilter
      {
        Slug = "project-info",
        Name = "Project Information",
        Icon = "Information",
        Values = projectInfo,
        Description = "Adds the selected project information as views to the stream"
      },
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
        if (deselect)
          obj.Select(false, true, false, true, true, true);
        else
          obj.Select(true, true, true, true, true, true);
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

  public override async Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(
    Dictionary<string, List<MappingValue>> Mapping
  )
  {
    await Task.Delay(TimeSpan.FromMilliseconds(500));
    return new Dictionary<string, List<MappingValue>>();
  }

  #endregion

  #region receiving

  public override bool CanPreviewReceive => true;

  private static bool IsPreviewIgnore(Base @object)
  {
    return @object.speckle_type.Contains("Instance")
      || @object.speckle_type.Contains("View")
      || @object.speckle_type.Contains("Collection");
  }

  public override async Task<StreamState> PreviewReceive(StreamState state, ProgressViewModel progress)
  {
    // first check if commit is the same and preview objects have already been generated
    Commit commit = await ConnectorHelpers.GetCommitFromState(progress.CancellationToken, state);
    progress.Report = new ProgressReport();

    if (commit.id != SelectedReceiveCommit)
    {
      // check for converter
      var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
      converter.SetContextDocument(Doc);

      var commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);

      SelectedReceiveCommit = commit.id;
      ClearStorage();

      var commitLayerName = DesktopUI2.Formatting.CommitInfo(state.CachedStream.name, state.BranchName, commit.id); // get commit layer name
      Preview = FlattenCommitObject(commitObject, converter);
      Doc.Notes += "%%%" + commitLayerName; // give converter a way to access commit layer info

      // Convert preview objects
      foreach (var previewObj in Preview)
      {
        previewObj.CreatedIds = new List<string> { previewObj.OriginalId }; // temporary store speckle id as created id for Preview report selection to work

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
            previewObj.Update(
              status: ApplicationObject.State.Created,
              logItem: $"Creating with {convertedFallback.Count()} fallback values"
            );
          else
            previewObj.Update(
              status: ApplicationObject.State.Failed,
              logItem: "Couldn't convert object or any fallback values"
            );
        }
        else
        {
          previewObj.Status = ApplicationObject.State.Created;
        }

        progress.Report.Log(previewObj);
      }
      progress.Report.Merge(converter.Report);

      // undo notes edit
      var segments = Doc.Notes.Split(new[] { "%%%" }, StringSplitOptions.None).ToList();
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

    if (progress.CancellationToken.IsCancellationRequested)
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
    var converter = KitManager.GetDefaultKit().LoadConverter(Utils.RhinoAppName);
    converter.SetContextDocument(Doc);
    converter.ReceiveMode = state.ReceiveMode;

    Commit commit = await ConnectorHelpers.GetCommitFromState(progress.CancellationToken, state);
    state.LastCommit = commit;

    if (SelectedReceiveCommit != commit.id)
    {
      ClearStorage();
      SelectedReceiveCommit = commit.id;
    }

    progress.Report = new ProgressReport();
    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 0;

    Base commitObject = null;
    if (Preview.Count == 0)
    {
      commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
      await ConnectorHelpers.TryCommitReceived(progress.CancellationToken, state, commit, Utils.RhinoAppName);
    }

    // get commit layer name
    var undoRecord = Doc.BeginUndoRecord($"Speckle bake operation for {state.CachedStream.name}");
    var commitLayerName = DesktopUI2.Formatting.CommitInfo(state.CachedStream.name, state.BranchName, commit.id);
    RhinoApp.InvokeOnUiThread(
      (Action)(
        () =>
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
              converter.Report.Log(previewObj); // Log object so converter can access
              if (previewObj.Convertible)
              {
                var storedObj = StoredObjects[previewObj.OriginalId];
                if (storedObj == null)
                {
                  previewObj.Update(
                    status: ApplicationObject.State.Failed,
                    logItem: "Couldn't retrieve stored object from bindings"
                  );
                  continue;
                }

                isPreviewIgnore = IsPreviewIgnore(storedObj);
                if (!isPreviewIgnore)
                  previewObj.Converted = ConvertObject(storedObj, converter);
              }
              else
              {
                foreach (var fallback in previewObj.Fallback)
                {
                  var storedFallback = StoredObjects[fallback.OriginalId];
                  fallback.Converted = ConvertObject(storedFallback, converter);
                }
              }

              if (!isPreviewIgnore && (previewObj.Converted == null || previewObj.Converted.Count == 0))
              {
                var convertedFallback = previewObj.Fallback
                  .Where(o => o.Converted != null || o.Converted.Count > 0)
                  .ToList();
                if (convertedFallback.Any())
                  previewObj.Update(logItem: $"Creating with {convertedFallback.Count()} fallback values");
                else
                  previewObj.Update(
                    status: ApplicationObject.State.Failed,
                    logItem: "Couldn't convert object or any fallback values"
                  );
              }

              progress.Report.Log(previewObj);
              if (progress.CancellationToken.IsCancellationRequested)
                return;
            }

            progress.Report.Merge(converter.Report);
          }
          else
          {
            Preview.ForEach(o => o.Status = ApplicationObject.State.Unknown);
          }
          if (progress.Report.OperationErrorsCount != 0)
            return;

          #region layer creation

          RhinoDoc.LayerTableEvent -= RhinoDoc_LayerChange; // temporarily unsubscribe from layer handler since layer creation will trigger it.

          // sort by depth and create all the containers as layers first
          var layers = new Dictionary<string, Layer>();
          var containers = Preview
            .Select(o => o.Container)
            .Distinct()
            .ToList()
            .OrderBy(path => path.Count(c => c == ':'))
            .ToList();
          foreach (var container in containers)
          {
            var path =
              state.ReceiveMode == ReceiveMode.Create
                ? $"{commitLayerName}{Layer.PathSeparator}{container}"
                : container;
            Layer layer = null;

            // try to see if there's a collection object first
            var collection = Preview
              .Where(o => o.Container == container && o.Descriptor.Contains("Collection"))
              .FirstOrDefault();
            if (collection != null)
            {
              var storedCollection = StoredObjects[collection.OriginalId];
              storedCollection["path"] = path; // needed by converter
              converter.Report.Log(collection); // Log object so converter can access
              var convertedCollection = converter.ConvertToNative(storedCollection) as List<object>;
              if (convertedCollection != null && convertedCollection.Count > 0)
              {
                layer = convertedCollection.First() as Layer;
                Preview[Preview.IndexOf(collection)] = converter.Report.ReportObjects[collection.OriginalId];
              }
            }
            // otherwise create the layer here (old commits before collections implementations, or from apps not supporting collections)
            else
            {
              layer = Doc.GetLayer(path, true);
            }

            if (layer == null)
            {
              progress.Report.OperationErrors.Add(
                new Exception($"Could not create layer [{path}]. Objects will be placed on default layer.")
              );
              continue;
            }

            layers.Add(layer.FullPath, layer);
          }
          #endregion

          foreach (var previewObj in Preview)
          {
            if (previewObj.Status != ApplicationObject.State.Unknown)
              continue; // this has already been converted and baked

            var isUpdate = false;

            // check receive mode & if objects need to be removed from the document after bake (or received objs need to be moved layers)
            var toRemove = new List<RhinoObject>();
            if (state.ReceiveMode == ReceiveMode.Update)
            {
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
            }
            if (toRemove.Count() > 0)
              isUpdate = true;

            // find layer and bake
            previewObj.CreatedIds.Clear(); // clear created ids before bake because these may be speckle ids from the preview
            var path =
              state.ReceiveMode == ReceiveMode.Create
                ? $"{commitLayerName}{Layer.PathSeparator}{previewObj.Container}"
                : previewObj.Container;
            var layer = layers.ContainsKey(path) ? layers[path] : Doc.GetLayer("Default", true);

            if (previewObj.Convertible)
            {
              BakeObject(previewObj, converter, layer);
              previewObj.Status = !previewObj.CreatedIds.Any()
                ? ApplicationObject.State.Failed
                : isUpdate
                  ? ApplicationObject.State.Updated
                  : ApplicationObject.State.Created;
            }
            else
            {
              foreach (var fallback in previewObj.Fallback)
                BakeObject(fallback, converter, layer, previewObj);
              previewObj.Status =
                previewObj.Fallback.Count(o => o.Status == ApplicationObject.State.Failed) == previewObj.Fallback.Count
                  ? ApplicationObject.State.Failed
                  : isUpdate
                    ? ApplicationObject.State.Updated
                    : ApplicationObject.State.Created;
            }

            progress.Report.Log(previewObj);

            if (progress.CancellationToken.IsCancellationRequested)
              return;
            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);
          }
          progress.Report.Merge(converter.Report);

          RhinoDoc.LayerTableEvent += RhinoDoc_LayerChange; // reactivate the layer handler

          // undo notes edit
          var segments = Doc.Notes.Split(new[] { "%%%" }, StringSplitOptions.None).ToList();
          Doc.Notes = segments[0];
        }
      )
    );

    Doc.Views.Redraw();
    Doc.EndUndoRecord(undoRecord);

    return state;
  }

  // gets objects by id directly or by application id user string
  private List<RhinoObject> GetObjectsByApplicationId(string applicationId)
  {
    if (string.IsNullOrEmpty(applicationId))
      return new List<RhinoObject>();

    // first try to find the object by app id user string
    var match =
      Doc.Objects.Where(o => o.Attributes.GetUserString(ApplicationIdKey) == applicationId)?.ToList()
      ?? new List<RhinoObject>();

    // if nothing is found, look for the geom obj by its guid directly
    if (!match.Any())
      try
      {
        RhinoObject obj = Doc.Objects.FindId(new Guid(applicationId));
        if (obj != null)
          match.Add(obj);
      }
      catch { }

    return match;
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
        appObj.Update(logItem: "Found another object in this commit with the same id. Skipped other object"); //TODO check if we are actually ignoring duplicates, since we are returning the app object anyway...
      else
        StoredObjects.Add(@base.id, @base);

      if (parameters != null && !StoredObjectParams.ContainsKey(@base.id))
        StoredObjectParams.Add(@base.id, parameters);
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
      if (current is Collection && string.IsNullOrEmpty(containerId))
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
      var parameters = current["parameters"] as Base;
      if (fallbackMember != null)
      {
        var appObj = NewAppObj();
        var fallbackObjects = GraphTraversal
          .TraverseMember(fallbackMember)
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
      if (context.propName == null)
        return stringBuilder; // this was probably the base commit collection

      string objectLayerName = string.Empty;
      if (context.propName.ToLower() == "elements" && context.current is Collection coll)
        objectLayerName = coll.name;
      else if (context.propName.ToLower() != "elements") // this is for any other property on the collection. skip elements props in layer structure.
        objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;
      LayerIdRecurse(context.parent, stringBuilder);
      if (stringBuilder.Length != 0 && !string.IsNullOrEmpty(objectLayerName))
        stringBuilder.Append(Layer.PathSeparator);
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

  private void BakeObject(
    ApplicationObject appObj,
    ISpeckleConverter converter,
    Layer layer,
    ApplicationObject parent = null
  )
  {
    var obj = StoredObjects[appObj.OriginalId];
    int bakedCount = 0;
    // check if this is a view or block - convert instead of bake if so (since these are "baked" during conversion)
    if (IsPreviewIgnore(obj))
      appObj.Converted = ConvertObject(obj, converter);
    foreach (var convertedItem in appObj.Converted)
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
          attributes.LayerIndex = layer.Index;

          // handle user info, including application id
          SetUserInfo(obj, attributes, parent);

          Guid id = Doc.Objects.Add(o, attributes);
          if (id == Guid.Empty)
          {
            var bakeMessage = "Could not bake to document.";
            if (parent != null)
              parent.Update(logItem: $"fallback {appObj.applicationId}: {bakeMessage}");
            else
              appObj.Update(logItem: bakeMessage);
            continue;
          }

          if (parent != null)
            parent.Update(id.ToString());
          else
            appObj.Update(id.ToString());

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

          o.Attributes.LayerIndex = layer.Index; // assign layer
          SetUserInfo(obj, o.Attributes, parent); // handle user info, including application id
          o.CommitChanges();

          if (parent != null)
            parent.Update(o.Id.ToString());
          else
            appObj.Update(o.Id.ToString());
          bakedCount++;
          break;
        case ViewInfo o: // this is a view, baked during conversion
          if (parent != null)
            parent.Update(o.Name);
          else
            appObj.Update(o.Name);
          bakedCount++;
          break;
      }

    if (bakedCount == 0)
    {
      if (parent != null)
        parent.Update(logItem: $"fallback {appObj.applicationId}: could not bake object");
      else
        appObj.Update(logItem: "Could not bake object");
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
      if (StoredObjectParams.ContainsKey(parent.OriginalId))
      {
        var parameters = StoredObjectParams[parent.OriginalId];
        foreach (var member in parameters.GetMembers(DynamicBaseMemberType.Dynamic))
          if (member.Value is Base parameter)
            try
            {
              attributes.SetUserString(member.Key, GetStringFromBaseProp(parameter, "value"));
            }
            catch { }
      }

    // set user dictionaries
    if (obj[UserDictionary] is Base userDictionary)
      ParseDictionaryToArchivable(attributes.UserDictionary, userDictionary);

    var name = obj["name"] as string;
    if (name != null)
      attributes.Name = name;
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

    converter.SetContextDocument(Doc);

    var filterObjs = GetObjectsFromFilter(state.Filter);
    var idsToSelect = new List<string>();
    int successful = 0;
    foreach (var id in filterObjs)
      if (Utils.FindObjectBySelectedId(Doc, id, out object obj, out string descriptor))
      {
        // create applicationObject
        ApplicationObject reportObj = new(id, descriptor);
        var applicationId = string.Empty;
        switch (obj)
        {
          case RhinoObject o:
            applicationId = o.Attributes.GetUserString(ApplicationIdKey) ?? id;
            if (converter.CanConvertToSpeckle(obj))
              reportObj.Update(status: ApplicationObject.State.Created);
            else
              reportObj.Update(
                status: ApplicationObject.State.Failed,
                logItem: "Object type conversion to Speckle not supported"
              );
            idsToSelect.Add(id);
            successful++;
            break;
          case Layer o:
            applicationId = o.GetUserString(ApplicationIdKey) ?? id;
            reportObj.Update(status: ApplicationObject.State.Created);
            successful++;
            break;
          case ViewInfo o:
            reportObj.Update(status: ApplicationObject.State.Created);
            successful++;
            break;
        }
        reportObj.applicationId = applicationId;
        progress.Report.Log(reportObj);
      }
      else
      {
        progress.Report.Log(
          new ApplicationObject(id, "Unknown")
          {
            Status = ApplicationObject.State.Failed,
            Log = new List<string> { "Could not find object in document" }
          }
        );
      }

    if (successful == 0)
      throw new InvalidOperationException("No valid objects selected, nothing will be sent!");

    // TODO: instead of selection, consider saving current visibility of objects in doc, hiding everything except selected, and restoring original states on cancel
    Doc.Objects.UnselectAll(false);
    SelectClientObjects(idsToSelect);
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
    var commitObject = converter.ConvertToSpeckle(Doc) as Collection; // create a collection base obj

    if (state.SelectedObjectIds.Count == 0)
      throw new InvalidOperationException(
        "Zero objects selected: Please select some objects, or check that your filter can actually select something."
      );

    progress.Report = new ProgressReport();
    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 0;

    progress.Max = state.SelectedObjectIds.Count;

    // store converted commit objects and layers by layer paths
    var commitLayerObjects = new Dictionary<string, List<Base>>();
    var commitLayers = new Dictionary<string, Layer>();
    var commitCollections = new Dictionary<string, Collection>();

    // convert all commit objs
    foreach (var selectedId in state.SelectedObjectIds)
    {
      progress.CancellationToken.ThrowIfCancellationRequested();

      Base converted = null;
      string applicationId = null;
      var reportObj = new ApplicationObject(selectedId, "Unknown");
      if (Utils.FindObjectBySelectedId(Doc, selectedId, out object obj, out string descriptor))
      {
        // create applicationObject
        reportObj = new ApplicationObject(selectedId, descriptor);
        converter.Report.Log(reportObj); // Log object so converter can access
        switch (obj)
        {
          case RhinoObject o:
            applicationId = o.Attributes.GetUserString(ApplicationIdKey) ?? selectedId;
            if (!converter.CanConvertToSpeckle(o))
            {
              reportObj.Update(
                status: ApplicationObject.State.Skipped,
                logItem: "Sending this object type is not supported in Rhino"
              );
              progress.Report.Log(reportObj);
              continue;
            }

            converted = converter.ConvertToSpeckle(o);

            if (converted != null)
            {
              var objectLayer = Doc.Layers[o.Attributes.LayerIndex];
              if (commitLayerObjects.ContainsKey(objectLayer.FullPath))
                commitLayerObjects[objectLayer.FullPath].Add(converted);
              else
                commitLayerObjects.Add(objectLayer.FullPath, new List<Base> { converted });
              if (!commitLayers.ContainsKey(objectLayer.FullPath))
                commitLayers.Add(objectLayer.FullPath, objectLayer);
            }
            break;
          case Layer o:
            applicationId = o.GetUserString(ApplicationIdKey) ?? selectedId;
            converted = converter.ConvertToSpeckle(o);
            if (converted is Collection layerCollection && !commitLayers.ContainsKey(o.FullPath))
            {
              commitLayers.Add(o.FullPath, o);
              commitCollections.Add(o.FullPath, layerCollection);
            }
            break;
          case ViewInfo o:
            converted = converter.ConvertToSpeckle(o);
            if (converted != null)
              commitObject.elements.Add(converted);
            break;
        }
      }
      else
      {
        progress.Report.LogOperationError(new Exception($"Failed to find doc object ${selectedId}."));
        continue;
      }

      if (converted == null)
      {
        reportObj.Update(status: ApplicationObject.State.Failed, logItem: "Conversion returned null");
        progress.Report.Log(reportObj);
        continue;
      }

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

    #region layer handling
    // convert layers as collections and attach all layer objects
    foreach (var layerPath in commitLayerObjects.Keys)
      if (commitCollections.ContainsKey(layerPath))
      {
        commitCollections[layerPath].elements = commitLayerObjects[layerPath];
      }
      else
      {
        var collection = converter.ConvertToSpeckle(commitLayers[layerPath]) as Collection;
        if (collection != null)
        {
          collection.elements = commitLayerObjects[layerPath];
          commitCollections.Add(layerPath, collection);
        }
      }

    // generate all parent paths of commit collections and create ordered list by depth descending
    var allPaths = new HashSet<string>();
    foreach (var key in commitLayers.Keys)
    {
      if (!allPaths.Contains(key))
        allPaths.Add(key);
      AddParent(commitLayers[key]);

      void AddParent(Layer childLayer)
      {
        var parentLayer = Doc.Layers.FindId(childLayer.ParentLayerId);
        if (parentLayer != null && !commitCollections.ContainsKey(parentLayer.FullPath))
        {
          var parentCollection = converter.ConvertToSpeckle(parentLayer) as Collection;
          if (parentCollection != null)
          {
            commitCollections.Add(parentLayer.FullPath, parentCollection);
            allPaths.Add(parentLayer.FullPath);
          }
          AddParent(parentLayer);
        }
      }
    }
    var orderedPaths = allPaths.OrderByDescending(path => path.Count(c => c == ':')).ToList(); // this ensures we attach children collections first

    // attach children collections to their parents and the base commit
    for (int i = 0; i < orderedPaths.Count; i++)
    {
      var path = orderedPaths[i];
      var collection = commitCollections[path];
      var parentIndex = path.LastIndexOf(Layer.PathSeparator);

      // if there is no parent, attach to base commit layer prop directly
      if (parentIndex == -1)
      {
        commitObject.elements.Add(collection);
        continue;
      }

      // get the parent collection, attach child, and update parent collection in commit collections
      var parentPath = path.Substring(0, parentIndex);
      var parent = commitCollections[parentPath];
      parent.elements.Add(commitCollections[path]);
      commitCollections[parentPath] = parent;
    }

    #endregion

    progress.Report.Merge(converter.Report);

    if (objCount == 0)
      throw new SpeckleException("Zero objects converted successfully. Send stopped.");

    progress.CancellationToken.ThrowIfCancellationRequested();

    progress.Max = objCount;

    var transports = new List<ITransport> { new ServerTransport(client.Account, streamId) };

    var objectId = await Operations.Send(
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
      objectId = objectId,
      branchName = state.BranchName,
      message = state.CommitMessage ?? $"Sent {objCount} elements from Rhino.",
      sourceApplication = Utils.RhinoAppName
    };

    if (state.PreviousCommitId != null)
      actualCommit.parents = new List<string> { state.PreviousCommitId };

    var commitId = await ConnectorHelpers.CreateCommit(progress.CancellationToken, client, actualCommit);
    return commitId;
  }

  private List<string> GetObjectsFromFilter(ISelectionFilter filter)
  {
    var objs = new List<string>();

    switch (filter.Slug)
    {
      case "manual":
        return filter.Selection;
      case "all":
        objs.AddRange(Doc.Layers.Select(o => o.Id.ToString()));
        objs.AddRange(Doc.Objects.Where(obj => obj.Visible).Select(obj => obj.Id.ToString()));
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
        if (filter.Selection.Contains("Layers"))
          objs.AddRange(Doc.Layers.Select(o => o.Id.ToString()));
        break;
    }

    return objs;
  }

  private string GetStringFromBaseProp(Base @base, string propName)
  {
    var val = @base[propName];
    if (val == null)
      return null;
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
  private void ParseDictionaryToArchivable(ArchivableDictionary target, Base @base)
  {
    foreach (var prop in @base.GetMemberNames())
    {
      var obj = @base[prop];
      switch (obj)
      {
        case Base o:
          var nested = new ArchivableDictionary();
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
