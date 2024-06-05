using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Converters.ArcGIS3.Utils;
using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Connectors.Utils;
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

  public (string path, Geometry converted, string? parentId) ConvertNonNativeGeometries(
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
    return ($"{string.Join("\\", objPath)}", converted, parentId);
  }

  public (string path, string converted) ConvertNativeLayers(Collection obj, string[] path, List<string> objectIds)
  {
    string converted = (string)_converter.Convert(obj);
    objectIds.Add(obj.id);
    string objPath = $"{string.Join("\\", path)}\\{obj.name}";
    return (objPath, converted);
  }

  public string AddDatasetsToMap((string, string) databaseObj)
  {
    try
    {
      return LayerFactory.Instance
        .CreateLayer(
          new Uri(
            $"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{databaseObj.Item2}"
          ),
          _contextStack.Current.Document.Map,
          layerName: databaseObj.Item1
        )
        .URI;
    }
    catch (ArgumentException)
    {
      return StandaloneTableFactory.Instance
        .CreateStandaloneTable(
          new Uri(
            $"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{databaseObj.Item2}"
          ),
          _contextStack.Current.Document.Map,
          tableName: databaseObj.Item1
        )
        .URI;
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

  public IReadOnlyList<ReceiveConversionResult> Build(
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
    Dictionary<string, (string path, Geometry converted, string? parentId)> convertedGeometries = new();
    List<string> objectIds = new();
    List<(string path, string converted)> convertedGISObjects = new();

    // 1. convert everything
    List<ReceiveConversionResult> results = new(objectsToConvert.Count);
    foreach (var item in objectsToConvert)
    {
      (string[] path, Base obj, string? parentId) = item;
      cancellationToken.ThrowIfCancellationRequested();
      try
      {
        if (obj is VectorLayer or Objects.GIS.RasterLayer)
        {
          var result = ConvertNativeLayers((Collection)obj, path, objectIds);
          convertedGISObjects.Add(result);
          results.Add(new(obj, result.converted, result.path));
        }
        else
        {
          var result = ConvertNonNativeGeometries(obj, path, parentId, objectIds);
          convertedGeometries[obj.id] = result;

          results.Add(new(obj, result.converted, result.path)); //POC: what native id?, path may not be unique
        }
      }
      catch (Exception ex) when (!ex.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        results.Add(new(obj, ex));
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
      AddDatasetsToMap(databaseObj);
      onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / convertedGISObjects.Count);
    }

    return results;
  }
}
