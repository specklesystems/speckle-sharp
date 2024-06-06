using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Converters.ArcGIS3.Utils;
using ArcGIS.Core.Geometry;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Core.Models.GraphTraversal;
using Speckle.Converters.ArcGIS3;

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

  public (string layerNestedPath, string datasetId) ConvertNativeLayers(
    Collection obj,
    string[] path,
    List<string> objectIds
  )
  {
    string datasetId = (string)_converter.Convert(obj);
    objectIds.Add(obj.id);
    string objPath = $"{string.Join("\\", path)}\\{obj.name}";
    return (objPath, datasetId);
  }

  public string AddDatasetsToMap((string layerName, string datasetId) databaseObj)
  {
    string mapLayerURI;
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
    return mapLayerURI;
  }

  private void AddErrorsToReport(
    List<ReceiveConversionResult> results,
    List<(
      bool isGisLayer,
      bool status,
      Base obj,
      string? datasetId,
      int? rowIndexNonGis,
      Exception? exception
    )> resultTracker
  )
  {
    for (int i = resultTracker.Count - 1; i >= 0; i--)
    {
      var converted = resultTracker[i];
      if (converted.status is false)
      {
        results.Add(new(Status.ERROR, converted.obj, null, null, converted.exception));
        resultTracker.RemoveAt(i);
      }
      else
      {
        // missed objects, do something
      }
    }
  }

  private void ConstructReport(
    List<ReceiveConversionResult> results,
    string datasetId,
    string mapLayerURI,
    List<(
      bool isGisLayer,
      bool status,
      Base obj,
      string? datasetId,
      int? rowIndexNonGis,
      Exception? exception
    )> resultTracker,
    Map map
  )
  {
    for (int i = resultTracker.Count - 1; i >= 0; i--)
    {
      var converted = resultTracker[i];
      if (converted.status is false)
      {
        continue;
      }

      if (datasetId == converted.datasetId)
      {
        MapMember member = map.FindLayer(mapLayerURI);
        if (member is FeatureLayer featLayer)
        {
          // for GIS layers, just get created layer URI
          if (converted.isGisLayer is true)
          {
            results.Add(
              new(Status.SUCCESS, converted.obj, $"{mapLayerURI}", $"{featLayer.GetType()}: {featLayer.ShapeType}")
            );
            resultTracker.RemoveAt(i);
            return;
          }
          results.Add(
            new(
              Status.SUCCESS,
              converted.obj,
              $"{mapLayerURI}_speckleRowIndex_{converted.rowIndexNonGis}",
              $"{featLayer.GetType()}: {featLayer.ShapeType}"
            )
          );
          resultTracker.RemoveAt(i);
          return;
        }

        if (member is RasterLayer rasterLayer)
        {
          results.Add(new(Status.SUCCESS, converted.obj, $"{mapLayerURI}", $"{rasterLayer.GetType()}"));
          resultTracker.RemoveAt(i);
          return;
        }

        if (member is LasDatasetLayer pointcloudLayer)
        {
          results.Add(new(Status.SUCCESS, converted.obj, $"{mapLayerURI}", $"{pointcloudLayer.GetType()}"));
          resultTracker.RemoveAt(i);
          return;
        }

        StandaloneTable table = map.FindStandaloneTable(mapLayerURI);
        if (table != null)
        {
          results.Add(new(Status.SUCCESS, converted.obj, $"{mapLayerURI}", $"{table.GetType()}"));
          resultTracker.RemoveAt(i);
          return;
        }
      }
    }
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
    List<Objects.GIS.VectorLayer> vectorLayers = context
      .GetAscendantOfType<Objects.GIS.VectorLayer>()
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
        if (obj is Objects.GIS.VectorLayer or Objects.GIS.RasterLayer)
        {
          var result = ConvertNativeLayers((Collection)obj, path, objectIds);
          savedDatasets.Add(result);
          // NOTE: Dim doesn't really know what is what - is the result.path the id of the obj?
          // TODO: is the type in here basically a GIS Layer?
          resultTracker.Add((true, true, obj, result.datasetId, null, null));
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
      var layerMapURI = AddDatasetsToMap(dataset);
      bakedLayerIds.Add(layerMapURI);
      ConstructReport(results, dataset.datasetId, layerMapURI, resultTracker, _contextStack.Current.Document.Map);
      onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / savedDatasets.Count);
    }
    AddErrorsToReport(results, resultTracker);
    // TODO: validated a correct set regarding bakedobject ids
    // bakedLayerIds is just layer IDs, no row information!
    return new(bakedLayerIds, results);
  }
}
