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

  private void AddDatasetsToMap(ObjectConversionTracker conversionTracker)
  {
    string? datasetId = conversionTracker.DatasetId; // should not ne null here
    string nestedLayerName = conversionTracker.NestedLayerName;

    Uri uri = new($"{_contextStack.Current.Document.SpeckleDatabasePath.AbsolutePath.Replace('/', '\\')}\\{datasetId}");
    Map map = _contextStack.Current.Document.Map;
    try
    {
      var layer = LayerFactory.Instance.CreateLayer(uri, map, layerName: nestedLayerName);
      conversionTracker.AddConvertedMapMember(layer);
      conversionTracker.AddLayerURI(layer.URI);
    }
    catch (ArgumentException)
    {
      var table = StandaloneTableFactory.Instance.CreateStandaloneTable(uri, map, tableName: nestedLayerName);
      conversionTracker.AddConvertedMapMember(table);
      conversionTracker.AddLayerURI(table.URI);
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
          string nestedLayerPath = $"{string.Join("\\", path)}\\{((Collection)obj).name}";
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

    // 3. add layer and tables to the Table Of Content
    int bakeCount = 0;
    onOperationProgressed?.Invoke("Adding to Map", bakeCount);
    foreach (var item in conversionTracker)
    {
      cancellationToken.ThrowIfCancellationRequested();

      // BAKE OBJECTS HERE
      var tracker = item.Value;
      if (tracker.Exception != null)
      {
        results.Add(new(Status.ERROR, tracker.Base, null, null, tracker.Exception));
      }
      else
      {
        AddDatasetsToMap(tracker);
        conversionTracker[item.Key] = tracker; // not strictly necessary, just keeps the conversionTracker object updated
        bakedObjectIds.Add(tracker.MappedLayerURI == null ? "" : tracker.MappedLayerURI);

        // prioritize individual hostAppGeometry type, if available:
        if (tracker.HostAppGeom != null)
        {
          results.Add(
            new(Status.SUCCESS, tracker.Base, tracker.HostAppGeom.GetType().ToString(), tracker.MappedLayerURI)
          );
        }
        else
        {
          results.Add(
            new(Status.SUCCESS, tracker.Base, tracker.HostAppMapMember?.GetType().ToString(), tracker.MappedLayerURI)
          );
        }
      }
      onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / conversionTracker.Count);
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
