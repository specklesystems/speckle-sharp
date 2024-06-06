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
using System.Linq.Expressions;

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

  public (string path, Geometry converted, string? parentId, Base obj) ConvertNonNativeGeometries(
    Base obj,
    string[] path,
    string? parentId,
    List<string> objectIds
  )
  {
    Geometry converted = (Geometry)_converter.Convert(obj);
    objectIds.Add(obj.id);
    List<string> objPath = path.ToList();
    objPath.Add(obj.speckle_type.Split(".")[^1]);
    return ($"{string.Join("\\", objPath)}", converted, parentId, obj);
  }

  public (string path, string converted) ConvertNativeLayers(Collection obj, string[] path, List<string> objectIds)
  {
    string converted = (string)_converter.Convert(obj);
    objectIds.Add(obj.id);
    string objPath = $"{string.Join("\\", path)}\\{obj.name}";
    return (objPath, converted);
  }

  public ReceiveConversionResult AddDatasetsToMap(
    (string layerName, string datasetId) databaseObj,
    List<(
      bool isGisLayer,
      bool status,
      Base obj,
      string? datasetId,
      int? rowIndexNonGis,
      Exception? exception
    )> resultTracker,
    MapView mapView
  )
  {
    string mapLayerURI;
    ReceiveConversionResult result;

    try
    {
      mapLayerURI = LayerFactory.Instance
        .CreateLayer(
          new Uri(
            $"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{databaseObj.datasetId}"
          ),
          _contextStack.Current.Document.Map,
          layerName: databaseObj.layerName
        )
        .URI;
    }
    catch (ArgumentException)
    {
      mapLayerURI = StandaloneTableFactory.Instance
        .CreateStandaloneTable(
          new Uri(
            $"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{databaseObj.datasetId}"
          ),
          _contextStack.Current.Document.Map,
          tableName: databaseObj.layerName
        )
        .URI;
    }

    foreach (var converted in resultTracker)
    {
      if (converted.status is false)
      {
        continue;
      }

      if (databaseObj.datasetId == converted.datasetId)
      {
        MapMember member = mapView.Map.FindLayer(mapLayerURI);
        if (member is FeatureLayer featLayer)
        {
          // for GIS layers, just get created layer URI
          result = new(Status.SUCCESS, converted.obj, mapLayerURI, $"{featLayer.GetType()}: {featLayer.ShapeType}");
          if (converted.isGisLayer is false) { }
          return result;
        }
      }
    }

    return result;
  }

  private string[] GetLayerPath(TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath =
      collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();
    return reverseOrderPath.Reverse().ToArray();
  }

  private bool HasGISParent(TraversalContext context)
  {
    List<VectorLayer> vectorLayers = context
      .GetAscendantOfType<VectorLayer>()
      .Where(obj => obj != context.Current)
      .ToList();
    List<Objects.GIS.RasterLayer> rasterLayers = context
      .GetAscendantOfType<Objects.GIS.RasterLayer>()
      .Where(obj => obj != context.Current)
      .ToList();
    return vectorLayers.Count + rasterLayers.Count > 0;
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

    // POC: This is where we will define our receive strategy, or maybe later somewhere else according to some setting pass from UI?
    var objectsToConvert = _traverseFunction
      .Traverse(rootObject)
      .Where(ctx => HasGISParent(ctx) is false)
      .Select(ctx => (GetLayerPath(ctx), ctx.Current, ctx.Parent?.Current.id))
      .ToList();

    int allCount = objectsToConvert.Count;
    int count = 0;
    Dictionary<string, (string path, Geometry converted, string? parentId, Base baseObj)> convertedGeometries = new();
    List<string> objectIds = new();
    List<(string pathForLayerNesting, string datasetId)> savedDatasets = new();

    // 1. convert everything
    List<ReceiveConversionResult> results = new(objectsToConvert.Count);
    List<(
      bool isGisLayer,
      bool status,
      Base obj,
      string? datasetId,
      int? rowIndexNonGis,
      Exception? exception
    )> resultTracker = new();
    List<string> bakedLayerIds = new();
    foreach (var item in objectsToConvert)
    {
      (string[] path, Base obj, string? parentId) = item;
      cancellationToken.ThrowIfCancellationRequested();
      try
      {
        if (obj is VectorLayer or Objects.GIS.RasterLayer)
        {
          var result = ConvertNativeLayers((Collection)obj, path, objectIds);
          savedDatasets.Add(result);
          // NOTE: Dim doesn't really know what is what - is the result.path the id of the obj?
          // TODO: is the type in here basically a GIS Layer?
          resultTracker.Add((true, true, obj, result.path, null, null));
          // results.Add(new(Status.SUCCESS, obj, result.path, "GIS Layer"));
        }
        else
        {
          convertedGeometries[obj.id] = ConvertNonNativeGeometries(obj, path, parentId, objectIds);

          // NOTE: Dim doesn't really know what is what - is the result.path the id of the obj?
          // results.Add(new(Status.SUCCESS, obj, result.path, result.converted.GetType().ToString())); //POC: what native id?, path may not be unique
          // TODO: Do we need this here? I remember oguzhan saying something that selection/object highlighting is weird in arcgis (weird is subjective)
          // bakedObjectIds.Add(result.path);
        }
      }
      catch (Exception ex) when (!ex.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // results.Add(new(Status.ERROR, obj, null, null, ex));
        resultTracker.Add((false, false, obj, null, null, ex));
      }
      onOperationProgressed?.Invoke("Converting", (double)++count / allCount);
    }

    // 2. convert Database entries with non-GIS geometry datasets

    onOperationProgressed?.Invoke("Writing to Database", null);
    savedDatasets.AddRange(_nonGisFeaturesUtils.WriteGeometriesToDatasets(convertedGeometries, resultTracker));

    int bakeCount = 0;
    onOperationProgressed?.Invoke("Adding to Map", bakeCount);
    // 3. add layer and tables to the Table Of Content
    foreach (var dataset in savedDatasets)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // BAKE OBJECTS HERE
      // bakedLayerIds.
      results.Add(AddDatasetsToMap(dataset, resultTracker, _contextStack.Current.Document.Map));
      ////////////////////////////////////////////////////////// results.Add(new(Status.SUCCESS, obj, result.path, "GIS Layer"));
      onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / savedDatasets.Count);
    }

    // TODO: validated a correct set regarding bakedobject ids
    return new(bakedLayerIds, results);
  }
}
