using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace SpeckleRhino;

public partial class ConnectorBindingsRhino : ConnectorBindings
{
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
    var converter = KitManager.GetDefaultKit().LoadConverter(Utils.GetRhinoHostAppVersion());
    converter.SetContextDocument(Doc);
    converter.ReceiveMode = state.ReceiveMode;

    // set converter settings
    bool settingsChanged = CurrentSettings != state.Settings;
    CurrentSettings = state.Settings;
    var settings = GetSettingsDict(CurrentSettings);
    converter.SetConverterSettings(settings);

    Commit commit = await ConnectorHelpers.GetCommitFromState(state, progress.CancellationToken);
    state.LastCommit = commit;

    if (SelectedReceiveCommit != commit.id || settingsChanged) // clear storage if receiving a new commit, or if settings have changed!!
    {
      ClearStorage();
      SelectedReceiveCommit = commit.id;
    }

    progress.Report = new ProgressReport();
    var conversionProgressDict = new ConcurrentDictionary<string, int>();
    conversionProgressDict["Conversion"] = 0;

    // track object types for mixpanel logging
    Dictionary<string, int> typeCountDict = new();

    Base commitObject = null;
    if (Preview.Count == 0)
    {
      commitObject = await ConnectorHelpers.ReceiveCommit(commit, state, progress);
      await ConnectorHelpers.TryCommitReceived(
        state,
        commit,
        Utils.GetRhinoHostAppVersion(),
        progress.CancellationToken
      );
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
                {
                  previewObj.Converted = ConvertObject(storedObj, converter);
                }
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
                {
                  previewObj.Update(logItem: $"Creating with {convertedFallback.Count()} fallback values");
                }
                else
                {
                  previewObj.Update(
                    status: ApplicationObject.State.Failed,
                    logItem: "Couldn't convert object or any fallback values"
                  );
                }
              }

              progress.Report.Log(previewObj);
              if (progress.CancellationToken.IsCancellationRequested)
              {
                return;
              }
            }

            progress.Report.Merge(converter.Report);
          }
          else
          {
            Preview.ForEach(o => o.Status = ApplicationObject.State.Unknown);
          }
          if (progress.Report.OperationErrorsCount != 0)
          {
            return;
          }

          #region layer creation

          RhinoDoc.LayerTableEvent -= RhinoDoc_LayerChange; // temporarily unsubscribe from layer handler since layer creation will trigger it.

          // sort by depth and create all the containers as layers first
          var layers = new Dictionary<string, Layer>();
          var containers = Preview
            .Select(o => o.Container)
            .Distinct()
            .Where(o => !string.IsNullOrEmpty(o))
            .OrderBy(path => path.Count(c => c == ':'))
            .ToList();

          // if on create mode, make sure the parent commit layer is created first
          if (state.ReceiveMode == ReceiveMode.Create)
          {
            var commitLayer = Doc.GetLayer(commitLayerName, true);
            if (commitLayer == null)
            {
              progress.Report.OperationErrors.Add(
                new Exception($"Could not create base commit layer [{commitLayerName}]. Operation aborted.")
              );
              return;
            }
          }
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

            layers.Add(path, layer);
          }
          #endregion

          foreach (var previewObj in Preview)
          {
            // log received object type
            if (StoredObjects.TryGetValue(previewObj.OriginalId, out Base previewBaseObj))
            {
              typeCountDict.TryGetValue(previewBaseObj.speckle_type, out var currentCount);
              typeCountDict[previewBaseObj.speckle_type] = ++currentCount;
            }

            if (previewObj.Status != ApplicationObject.State.Unknown)
            {
              continue; // this has already been converted and baked
            }

            var isUpdate = false;

            // check receive mode & if objects need to be removed from the document after bake (or received objs need to be moved layers)
            var toRemove = new List<RhinoObject>();
            if (state.ReceiveMode == ReceiveMode.Update)
            {
              toRemove = GetObjectsByApplicationId(previewObj.applicationId);
              toRemove.ForEach(o => Doc.Objects.Delete(o, false, true));

              if (!toRemove.Any()) // if no rhinoobjects were found, this could've been a view or level (named construction plane)
              {
                // Check converter (ViewToNative and LevelToNative) to make sure these names correspond!
                var name =
                  state.ReceiveMode == ReceiveMode.Create
                    ? $"{commitLayerName} - {StoredObjects[previewObj.OriginalId]["name"] as string}"
                    : StoredObjects[previewObj.OriginalId]["name"] as string;
                var viewId = Doc.NamedViews.FindByName(name);
                var planeId = Doc.NamedConstructionPlanes.Find(name);
                if (viewId != -1)
                {
                  isUpdate = true;
                  Doc.NamedViews.Delete(viewId);
                }
                else if (planeId != -1)
                {
                  isUpdate = true;
                  Doc.NamedConstructionPlanes.Delete(planeId);
                }
              }
            }
            if (toRemove.Count() > 0)
            {
              isUpdate = true;
            }

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
              {
                BakeObject(fallback, converter, layer, previewObj);
              }

              previewObj.Status =
                previewObj.Fallback.Count(o => o.Status == ApplicationObject.State.Failed) == previewObj.Fallback.Count
                  ? ApplicationObject.State.Failed
                  : isUpdate
                    ? ApplicationObject.State.Updated
                    : ApplicationObject.State.Created;
            }

            progress.Report.Log(previewObj);

            if (progress.CancellationToken.IsCancellationRequested)
            {
              return;
            }

            conversionProgressDict["Conversion"]++;
            progress.Update(conversionProgressDict);
          }
          progress.Report.Merge(converter.Report);

          RhinoDoc.LayerTableEvent += RhinoDoc_LayerChange; // reactivate the layer handler

          // track the object type counts as an event before we try to receive
          // this will tell us the composition of a commit the user is trying to convert and receive, even if it's not successfully converted or received
          // we are capped at 255 properties for mixpanel events, so we need to check dict entries
          var typeCountList = typeCountDict
            .Select(o => new { TypeName = o.Key, Count = o.Value })
            .OrderBy(pair => pair.Count)
            .Reverse()
            .Take(200);

          Speckle.Core.Logging.Analytics.TrackEvent(
            Speckle.Core.Logging.Analytics.Events.ConvertToNative,
            new Dictionary<string, object>() { { "typeCount", typeCountList } }
          );

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
    List<RhinoObject> match = new();

    if (string.IsNullOrEmpty(applicationId))
    {
      return match;
    }

    // first try to find the object by app id user string
    if (Doc.Objects.FindByUserString(ApplicationIdKey, applicationId, true) is RhinoObject[] foundObjects)
    {
      match = foundObjects.ToList();
    }

    // if nothing is found, look for the geom obj by its guid directly
    if (match.Count == 0)
    {
      if (Utils.GetGuidFromString(applicationId, out Guid id))
      {
        if (Doc.Objects.FindId(id) is RhinoObject obj)
        {
          match.Add(obj);
        }
      }
    }

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
      if (!StoredObjects.ContainsKey(@base.id))
      {
        StoredObjects.Add(@base.id, @base);
      }

      if (parameters != null && !StoredObjectParams.ContainsKey(@base.id))
      {
        StoredObjectParams.Add(@base.id, parameters);
      }
    }

    ApplicationObject CreateApplicationObject(Base current, string containerId)
    {
      ApplicationObject NewAppObj()
      {
        var speckleType = current.speckle_type
          .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
          .LastOrDefault();

        return new ApplicationObject(current.id, speckleType)
        {
          applicationId = current.applicationId,
          Container = containerId
        };
      }

      // skip if it is the base commit collection
      if (current is Collection && string.IsNullOrEmpty(containerId))
      {
        return null;
      }

      // get parameters and attributes from revit/gis
      var parameters = current["parameters"] as Base ?? current["attributes"] as Base;

      //Handle convertable objects
      if (converter.CanConvertToNative(current))
      {
        var appObj = NewAppObj();
        appObj.Convertible = true;
        StoreObject(current, appObj, parameters);
        return appObj;
      }

      //Handle objects convertable using displayValues
      var fallbackMember = DefaultTraversal.displayValuePropAliases
        .Where(o => current[o] != null)
        ?.Select(o => current[o])
        ?.FirstOrDefault();
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
      {
        return stringBuilder; // this was probably the base commit collection
      }

      // handle elements hosting case from Revit
      // WARNING: THIS IS REVIT-SPECIFIC CODE!!
      // We are checking for the `Category` prop on children objects to use as the layer
      if (
        DefaultTraversal.elementsPropAliases.Contains(context.propName)
        && context.parent.current is not Collection
        && !string.IsNullOrEmpty((string)context.current["category"])
      )
      {
        stringBuilder.Append((string)context.current["category"]);
        return stringBuilder;
      }

      string objectLayerName = string.Empty;

      // handle collections case
      if (context.current is Collection coll && DefaultTraversal.elementsPropAliases.Contains(context.propName))
      {
        objectLayerName = coll.name;
      }
      // handle default case
      else if (!DefaultTraversal.elementsPropAliases.Contains(context.propName))
      {
        objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;
      }

      LayerIdRecurse(context.parent, stringBuilder);
      if (stringBuilder.Length != 0 && !string.IsNullOrEmpty(objectLayerName))
      {
        stringBuilder.Append(Layer.PathSeparator);
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

  // conversion and bake
  private List<object> ConvertObject(Base obj, ISpeckleConverter converter)
  {
    var convertedList = new List<object>();

    var converted = converter.ConvertToNative(obj);
    if (converted == null)
    {
      return convertedList;
    }

    //Iteratively flatten any lists
    void FlattenConvertedObject(object item)
    {
      if (item is IList list)
      {
        foreach (object child in list)
        {
          FlattenConvertedObject(child);
        }
      }
      else
      {
        convertedList.Add(item);
      }
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
            {
              parent.Update(logItem: $"fallback {appObj.applicationId}: {invalidMessage}");
            }
            else
            {
              appObj.Update(logItem: invalidMessage);
            }

            continue;
          }

          // create attributes with layer, display and render, user info, application id, revit/gis parameters
          ObjectAttributes attributes = CreateAttributesFromObject(
            obj,
            layer.Index,
            converter,
            out RenderMaterial renderMaterial,
            parent
          );

          Guid id = Doc.Objects.Add(o, attributes);
          if (id == Guid.Empty)
          {
            var bakeMessage = "Could not bake to document.";
            if (parent != null)
            {
              parent.Update(logItem: $"fallback {appObj.applicationId}: {bakeMessage}");
            }
            else
            {
              appObj.Update(logItem: bakeMessage);
            }

            continue;
          }

          if (parent != null)
          {
            parent.Update(id.ToString());
          }
          else
          {
            appObj.Update(id.ToString());
          }

          bakedCount++;

          // handle render material
          if (renderMaterial is not null)
          {
            RhinoObject rhinoObject = Doc.Objects.FindId(id);
            rhinoObject.RenderMaterial = renderMaterial;
            rhinoObject.CommitChanges();
          }

          break;

        case RhinoObject o: // this was prbly a block instance, baked during conversion
          // create attributes with layer, display and render, user info, application id, revit/gis parameters
          ObjectAttributes objectAttributes = CreateAttributesFromObject(
            obj,
            layer.Index,
            converter,
            out RenderMaterial objectRenderMaterial,
            parent
          );

          o.Attributes = objectAttributes;
          o.CommitChanges();
          if (parent != null)
          {
            parent.Update(o.Id.ToString());
          }
          else
          {
            appObj.Update(o.Id.ToString());
          }

          bakedCount++;
          break;

        case Group o: // this is a GIS object
          // create attributes with layer, display and render, user info, application id, revit/gis parameters
          ObjectAttributes groupAttributes = CreateAttributesFromObject(
            obj,
            layer.Index,
            converter,
            out RenderMaterial groupRenderMaterial,
            parent
          );

          groupAttributes.AddToGroup(o.Index);

          foreach (RhinoObject groupObject in Doc.Objects.FindByGroup(o.Index))
          {
            groupObject.Attributes = groupAttributes;

            // handle render material
            if (groupRenderMaterial is not null)
            {
              groupObject.RenderMaterial = groupRenderMaterial;
            }

            groupObject.CommitChanges();
          }

          if (parent != null)
          {
            parent.Update(o.Id.ToString());
          }
          else
          {
            appObj.Update(o.Id.ToString());
          }
          bakedCount++;
          break;

        case ViewInfo o: // this is a view, baked during conversion
          appObj.Update(o.Name);
          bakedCount++;
          break;

        case ConstructionPlane o: // this is a level, baked during conversion
          appObj.Update(o.Name);
          bakedCount++;
          break;
      }
    }

    if (bakedCount == 0)
    {
      if (parent != null)
      {
        parent.Update(logItem: $"fallback {appObj.applicationId}: could not bake object");
      }
      else
      {
        appObj.Update(logItem: "Could not bake object");
      }
    }
  }

  private ObjectAttributes CreateAttributesFromObject(
    Base obj,
    int layerIndex,
    ISpeckleConverter converter,
    out RenderMaterial renderMaterial,
    ApplicationObject parent = null
  )
  {
    ObjectAttributes attributes = new();
    renderMaterial = null;

    // handle display style
    Base display = obj["displayStyle"] as Base ?? obj["@displayStyle"] as Base;
    Base render = obj["renderMaterial"] as Base ?? obj["@renderMaterial"] as Base;
    if (display != null)
    {
      var convertedDisplay = converter.ConvertToNative(display) as ObjectAttributes;
      if (convertedDisplay is not null)
      {
        attributes = convertedDisplay;
      }
    }
    else if (render != null)
    {
      attributes.ColorSource = ObjectColorSource.ColorFromMaterial;
    }

    // assign layer
    attributes.LayerIndex = layerIndex;

    // set user strings
    if (obj[UserStrings] is Base userStrings)
    {
      foreach (var member in userStrings.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        attributes.SetUserString(member.Key, member.Value as string);
      }
    }

    // set application id
    var appId = parent != null ? parent.applicationId : obj.applicationId;
    attributes.SetUserString(ApplicationIdKey, appId);

    // set user dictionaries
    if (obj[UserDictionary] is Base userDictionary)
    {
      ParseDictionaryToArchivable(attributes.UserDictionary, userDictionary);
    }

    // set name or label
    var name = obj["name"] as string ?? obj["label"] as string; // gridlines have a "label" prop instead of name?
    if (name != null)
    {
      attributes.Name = name;
    }

    // set revit/gis parameters as user strings
    var paramId = parent != null ? parent.OriginalId : obj.id;
    if (StoredObjectParams.TryGetValue(paramId, out Base parameters))
    {
      foreach (var member in parameters.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        // parameters coming from revit, value Base is Objects.BuiltElements.Revit.Parameter
        if (member.Value is Base parameter)
        {
          var convertedParameter = converter.ConvertToNative(parameter) as Tuple<string, string>;
          if (convertedParameter is not null)
          {
            var paramName = $"{convertedParameter.Item1}({member.Key})";
            attributes.SetUserString(paramName, convertedParameter.Item2);
          }
        }
        // attributes coming from GIS
        else
        {
          string userStringValue = member.Value is object value ? value.ToString() : string.Empty;
          attributes.SetUserString(member.Key, userStringValue);
        }
      }
    }

    // render material
    if (render is not null)
    {
      renderMaterial = converter.ConvertToNative(render) as RenderMaterial;
    }

    return attributes;
  }

  // Clears the stored objects, params, and preview objects
  private void ClearStorage()
  {
    Preview.Clear();
    StoredObjects.Clear();
    StoredObjectParams.Clear();
  }

  /// <summary>
  /// Copies a Base to an ArchivableDictionary
  /// </summary>
  /// <param name="target"></param>
  private void ParseDictionaryToArchivable(ArchivableDictionary target, Base @base)
  {
    foreach (var prop in @base.GetMembers().Keys)
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
}
