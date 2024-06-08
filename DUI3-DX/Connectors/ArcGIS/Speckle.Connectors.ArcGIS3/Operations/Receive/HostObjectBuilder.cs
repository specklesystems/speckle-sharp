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

  private (string path, Geometry converted) ConvertNonNativeGeometries(Base obj, string[] path)
  {
    Geometry converted = (Geometry)_converter.Convert(obj);
    List<string> objPath = path.ToList();
    objPath.Add(obj.speckle_type.Split(".")[^1]);
    return (string.Join("\\", objPath), converted);
  }

  private (string path, string converted) ConvertNativeLayers(Collection obj, string[] path)
  {
    string converted = (string)_converter.Convert(obj);
    string objPath = $"{string.Join("\\", path)}\\{obj.name}";
    return (objPath, converted);
  }

  private string AddDatasetsToMap((string nestedLayerName, string datasetId) databaseObj)
  {
    Uri uri =
      new(
        $"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{databaseObj.datasetId}"
      );
    Map map = _contextStack.Current.Document.Map;
    try
    {
      return LayerFactory.Instance.CreateLayer(uri, map, layerName: databaseObj.nestedLayerName).URI;
    }
    catch (ArgumentException)
    {
      return StandaloneTableFactory.Instance
        .CreateStandaloneTable(uri, map, tableName: databaseObj.nestedLayerName)
        .URI;
    }
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

    var objectsToConvert = _traverseFunction
      .Traverse(rootObject)
      .Where(ctx => ctx.Current is not Collection || IsGISType(ctx.Current))
      .Where(ctx => HasGISParent(ctx) is false)
      .ToList();

    int allCount = objectsToConvert.Count;
    int count = 0;
    Dictionary<TraversalContext, (string path, Geometry converted)> convertedGeometries = new();
    List<(string path, string converted)> convertedGISObjects = new();

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
          var result = ConvertNativeLayers((Collection)obj, path);
          convertedGISObjects.Add(result);
          // NOTE: Dim doesn't really know what is what - is the result.path the id of the obj?
          // TODO: is the type in here basically a GIS Layer?
          results.Add(new(Status.SUCCESS, obj, result.path, "GIS Layer"));
        }
        else
        {
          var result = ConvertNonNativeGeometries(obj, path);
          convertedGeometries[ctx] = result;

          // NOTE: Dim doesn't really know what is what - is the result.path the id of the obj?
          results.Add(new(Status.SUCCESS, obj, result.path, result.converted.GetType().ToString())); //POC: what native id?, path may not be unique
          // TODO: Do we need this here? I remember oguzhan saying something that selection/object highlighting is weird in arcgis (weird is subjective)
          // bakedObjectIds.Add(result.path);
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
    convertedGISObjects.AddRange(_nonGisFeaturesUtils.WriteGeometriesToDatasets(convertedGeometries));

    int bakeCount = 0;
    onOperationProgressed?.Invoke("Adding to Map", bakeCount);
    // 3. add layer and tables to the Table Of Content
    foreach (var databaseObj in convertedGISObjects)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // BAKE OBJECTS HERE
      bakedObjectIds.Add(AddDatasetsToMap(databaseObj));
      onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / convertedGISObjects.Count);
    }

    // TODO: validated a correct set regarding bakedobject ids
    return new(bakedObjectIds, results);
  }

  [Pure]
  private static string[] GetLayerPath(TraversalContext context)
  {
    string[] collectionBasedPath = context.GetAscendantOfType<Collection>().Select(c => c.name).ToArray();
    string[] reverseOrderPath =
      collectionBasedPath.Length != 0 ? collectionBasedPath : context.GetPropertyPath().ToArray();
    return reverseOrderPath.Reverse().ToArray();
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
