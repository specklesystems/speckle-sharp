using System.Diagnostics.Contracts;
using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Converters.ArcGIS3.Utils;
using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Converters.ArcGIS3;
using RasterLayer = Objects.GIS.RasterLayer;

namespace Speckle.Connectors.ArcGIS.Operations.Receive;

public class ArcGISHostObjectBuilder : IHostObjectBuilder
{
  private readonly IRootToHostConverter _converter;
  private readonly INonNativeFeaturesUtils _nonGisFeaturesUtils;

  // POC: figure out the correct scope to only initialize on Receive
  private readonly IConversionContextStack<ArcGISDocument, Unit> _contextStack;
  private readonly GraphTraversal _traverseFunction;

  public ArcGISHostObjectBuilder(
    IRootToHostConverter converter,
    IConversionContextStack<ArcGISDocument, Unit> contextStack,
    INonNativeFeaturesUtils nonGisFeaturesUtils,
    GraphTraversal traverseFunction
  )
  {
    _converter = converter;
    _contextStack = contextStack;
    _nonGisFeaturesUtils = nonGisFeaturesUtils;
    _traverseFunction = traverseFunction;
  }

  public HostObjectBuilderResult Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // Prompt the UI conversion started. Progress bar will swoosh.
    onOperationProgressed?.Invoke("Converting", null);

    // Browse for any trace of geolocation in non-GIS apps (e.g. Revit: implemented, Blender: todo on Blender side, Civil3d: ?)
    CRSorigin? dataOrigin = null; // e.g. CRSorigin.FromRevitData(rootObject);

    // save SpatialReference (overwrite default Map.SpatialRef), use it for dataset writing
    if (dataOrigin is CRSorigin crsOrigin)
    {
      SpatialReference incomingSpatialRef = crsOrigin.CreateCustomCRS();
      _contextStack.Current.Document.IncomingSpatialReference = incomingSpatialRef;
    }

    var objectsToConvert = _traverseFunction
      .Traverse(rootObject)
      .Where(ctx => ctx.Current is not Collection || IsGISType(ctx.Current))
      .Where(ctx => HasGISParent(ctx) is false)
      .ToList();

    int allCount = objectsToConvert.Count;
    int count = 0;
    Dictionary<TraversalContext, ObjectConversionTracker> conversionTracker = new();

    // 1. convert everything
    List<ReceiveConversionResult> results = new(objectsToConvert.Count);
    List<string> bakedObjectIds = new();
    foreach (TraversalContext ctx in objectsToConvert)
    {
      string[] path = GetLayerPath(ctx);
      Base obj = ctx.Current;

      cancellationToken.ThrowIfCancellationRequested();
      try
      {
        if (IsGISType(obj))
        {
          string nestedLayerPath = $"{string.Join("\\", path)}";
          string datasetId = (string)_converter.Convert(obj);
          conversionTracker[ctx] = new ObjectConversionTracker(obj, nestedLayerPath, datasetId);
        }
        else
        {
          string nestedLayerPath = $"{string.Join("\\", path)}\\{obj.speckle_type.Split(".")[^1]}";
          Geometry converted = (Geometry)_converter.Convert(obj);
          conversionTracker[ctx] = new ObjectConversionTracker(obj, nestedLayerPath, converted);
        }
      }
      catch (Exception ex) when (!ex.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        results.Add(new(Status.ERROR, obj, null, null, ex));
      }
      onOperationProgressed?.Invoke("Converting", (double)++count / allCount);
    }

    // 2. convert Database entries with non-GIS geometry datasets
    onOperationProgressed?.Invoke("Writing to Database", null);
    _nonGisFeaturesUtils.WriteGeometriesToDatasets(conversionTracker);

    // Create main group layer
    Dictionary<string, GroupLayer> createdLayerGroups = new();
    Map map = _contextStack.Current.Document.Map;
    GroupLayer groupLayer = LayerFactory.Instance.CreateGroupLayer(map, 0, $"{projectName}: {modelName}");
    createdLayerGroups["Basic Speckle Group"] = groupLayer; // key doesn't really matter here

    // 3. add layer and tables to the Table Of Content
    int bakeCount = 0;
    Dictionary<string, MapMember> bakedMapMembers = new();
    onOperationProgressed?.Invoke("Adding to Map", bakeCount);

    foreach (var item in conversionTracker)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var trackerItem = conversionTracker[item.Key]; // updated tracker object

      // BAKE OBJECTS HERE
      if (trackerItem.Exception != null)
      {
        results.Add(new(Status.ERROR, trackerItem.Base, null, null, trackerItem.Exception));
      }
      else if (trackerItem.DatasetId == null)
      {
        results.Add(
          new(Status.ERROR, trackerItem.Base, null, null, new ArgumentException("Unknown error: Dataset not created"))
        );
      }
      else if (bakedMapMembers.TryGetValue(trackerItem.DatasetId, out MapMember? value))
      {
        // only add a report item
        AddResultsFromTracker(trackerItem, results);
      }
      else
      {
        // add layer and layer URI to tracker
        MapMember mapMember = AddDatasetsToMap(trackerItem, createdLayerGroups);
        trackerItem.AddConvertedMapMember(mapMember);
        trackerItem.AddLayerURI(mapMember.URI);
        conversionTracker[item.Key] = trackerItem;

        // add layer URI to bakedIds
        bakedObjectIds.Add(trackerItem.MappedLayerURI == null ? "" : trackerItem.MappedLayerURI);

        // mark dataset as already created
        bakedMapMembers[trackerItem.DatasetId] = mapMember;

        // add report item
        AddResultsFromTracker(trackerItem, results);
      }
      onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / conversionTracker.Count);
    }
    bakedObjectIds.AddRange(createdLayerGroups.Values.Select(x => x.URI));

    // TODO: validated a correct set regarding bakedobject ids
    return new(bakedObjectIds, results);
  }

  private void AddResultsFromTracker(ObjectConversionTracker trackerItem, List<ReceiveConversionResult> results)
  {
    // prioritize individual hostAppGeometry type, if available:
    if (trackerItem.HostAppGeom != null)
    {
      results.Add(
        new(Status.SUCCESS, trackerItem.Base, trackerItem.MappedLayerURI, trackerItem.HostAppGeom.GetType().ToString())
      );
    }
    else
    {
      results.Add(
        new(
          Status.SUCCESS,
          trackerItem.Base,
          trackerItem.MappedLayerURI,
          trackerItem.HostAppMapMember?.GetType().ToString()
        )
      );
    }
  }

  private MapMember AddDatasetsToMap(
    ObjectConversionTracker trackerItem,
    Dictionary<string, GroupLayer> createdLayerGroups
  )
  {
    // get layer details
    string? datasetId = trackerItem.DatasetId; // should not ne null here
    Uri uri = new($"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{datasetId}");
    string nestedLayerName = trackerItem.NestedLayerName;

    // add group for the current layer
    string shortName = nestedLayerName.Split("\\")[^1];
    string nestedLayerPath = string.Join("\\", nestedLayerName.Split("\\").SkipLast(1));

    GroupLayer groupLayer = CreateNestedGroupLayer(nestedLayerPath, createdLayerGroups);

    // Most of the Speckle-written datasets will be containing geometry and added as Layers
    // although, some datasets might be just tables (e.g. native GIS Tables, in the future maybe Revit schedules etc.
    // We can create a connection to the dataset in advance and determine its type, but this will be more
    // expensive, than assuming by default that it's a layer with geometry (which in most cases it's expected to be)
    try
    {
      var layer = LayerFactory.Instance.CreateLayer(uri, groupLayer, layerName: shortName);
      layer.SetExpanded(true);
      return layer;
    }
    catch (ArgumentException)
    {
      var table = StandaloneTableFactory.Instance.CreateStandaloneTable(uri, groupLayer, tableName: shortName);
      return table;
    }
  }

  private GroupLayer CreateNestedGroupLayer(string nestedLayerPath, Dictionary<string, GroupLayer> createdLayerGroups)
  {
    GroupLayer lastGroup = createdLayerGroups.FirstOrDefault().Value;
    if (lastGroup == null) // if layer not found
    {
      throw new InvalidOperationException("Speckle Layer Group not found");
    }

    // iterate through each nested level
    string createdGroupPath = "";
    var allPathElements = nestedLayerPath.Split("\\").Where(x => !string.IsNullOrEmpty(x));
    foreach (string pathElement in allPathElements)
    {
      createdGroupPath += "\\" + pathElement;
      if (createdLayerGroups.TryGetValue(createdGroupPath, out var existingGroupLayer))
      {
        lastGroup = existingGroupLayer;
      }
      else
      {
        // create new GroupLayer under last found Group, named with last pathElement
        lastGroup = LayerFactory.Instance.CreateGroupLayer(lastGroup, 0, pathElement);
        lastGroup.SetExpanded(true);
      }
      createdLayerGroups[createdGroupPath] = lastGroup;
    }
    return lastGroup;
  }

  [Pure]
  private static string[] GetLayerPath(TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath =
      collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();

    var originalPath = reverseOrderPath.Reverse().ToArray();
    return originalPath.Where(x => !string.IsNullOrEmpty(x)).ToArray();
  }

  [Pure]
  private static bool HasGISParent(TraversalContext context)
  {
    List<Base> gisLayers = context.GetAscendants().Where(IsGISType).Where(obj => obj != context.Current).ToList();
    return gisLayers.Count > 0;
  }

  [Pure]
  private static bool IsGISType(Base obj)
  {
    return obj is RasterLayer or VectorLayer;
  }
}
