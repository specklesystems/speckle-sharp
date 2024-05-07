using System.Diagnostics;
using ArcGIS.Desktop.Mapping;
using Speckle.Connectors.Utils.Builders;
using Speckle.Converters.Common;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using Speckle.Converters.ArcGIS3.Utils;
using ArcGIS.Core.Geometry;
using Objects.GIS;
using Speckle.Converters.Common.Objects;

namespace Speckle.Connectors.ArcGIS.Operations.Receive;

public class HostObjectBuilder : IHostObjectBuilder
{
  private readonly ISpeckleConverterToHost _toHostConverter;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;
  private readonly IRawConversion<
    Dictionary<string, Tuple<List<string>, Geometry>>,
    List<Tuple<string, string>>
  > _nonGISToHostConverter;

  // POC: figure out the correct scope to only initialize on Receive
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public HostObjectBuilder(
    ISpeckleConverterToHost toHostConverter,
    IArcGISProjectUtils arcGISProjectUtils,
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<Dictionary<string, Tuple<List<string>, Geometry>>, List<Tuple<string, string>>> nonGISToHostConverter
  )
  {
    _toHostConverter = toHostConverter;
    _arcGISProjectUtils = arcGISProjectUtils;
    _contextStack = contextStack;
    _nonGISToHostConverter = nonGISToHostConverter;
  }

  public IEnumerable<string> Build(
    Base rootObject,
    string projectName,
    string modelName,
    Action<string, double?>? onOperationProgressed,
    CancellationToken cancellationToken
  )
  {
    // Prompt the UI conversion started. Progress bar will swoosh.
    onOperationProgressed?.Invoke("Converting", null);

    // create and add Geodatabase to a project

    string databasePath = _arcGISProjectUtils.GetDatabasePath();
    _arcGISProjectUtils.AddDatabaseToProject(databasePath);

    // POC: This is where we will define our receive strategy, or maybe later somewhere else according to some setting pass from UI?
    IEnumerable<(string[], Base)> objectsWithPath = rootObject.TraverseWithPath((obj) => obj is not Collection);

    IEnumerable<(string[], Base)> gisObjectsWithPath = objectsWithPath.Where(
      x => x.Item2 is VectorLayer || x.Item2 is Objects.GIS.RasterLayer
    );
    IEnumerable<(string[], Base)> nonGisObjectsWithPath = objectsWithPath.Where(
      x => x.Item2 is not GisFeature && x.Item2 is not VectorLayer && x.Item2 is not Objects.GIS.RasterLayer
    );

    List<string> objectIds = new();
    int count = 0;
    int allCount = objectsWithPath.Count();

    Dictionary<string, Tuple<List<string>, Geometry>> convertedGeometries = new();
    List<Tuple<string, string>> convertedGISObjects = new();

    // 1. convert non-gis objects in a loop
    foreach ((string[] path, Base obj) in nonGisObjectsWithPath)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        throw new OperationCanceledException(cancellationToken);
      }
      try
      {
        // POC: QueuedTask
        QueuedTask.Run(() =>
        {
          Geometry converted = (Geometry)_toHostConverter.Convert(obj);
          objectIds.Add(obj.id);
          List<string> objPath = path.ToList();
          objPath.Add(obj.speckle_type.Split(".")[^1]);
          convertedGeometries[obj.id] = Tuple.Create(objPath, converted);
        });

        onOperationProgressed?.Invoke("Converting", (double)++count / allCount);
      }
      catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }
    // convert Database entries with non-GIS geometry datasets
    try
    {
      onOperationProgressed?.Invoke("Writing to Database", null);
      convertedGISObjects.AddRange(_nonGISToHostConverter.RawConvert(convertedGeometries));
    }
    catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
    {
      // POC: report, etc.
      Debug.WriteLine("conversion error happened.");
    }

    // 2. convert gis-objects in a loop
    foreach ((string[] path, Base obj) in gisObjectsWithPath)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        throw new OperationCanceledException(cancellationToken);
      }
      try
      {
        // POC: QueuedTask
        var task = QueuedTask.Run(() =>
        {
          string converted = (string)_toHostConverter.Convert(obj);
          objectIds.Add(obj.id);
          string objPath = $"{string.Join("\\", path)}\\{((Collection)obj).name}";
          convertedGISObjects.Add(Tuple.Create(objPath, converted));
        });
        task.Wait(cancellationToken);

        onOperationProgressed?.Invoke("Converting", (double)++count / allCount);
      }
      catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }

    int bakeCount = 0;
    onOperationProgressed?.Invoke("Adding to Map", 0);
    // 3. add layer and tables to the Table Of Content
    foreach (Tuple<string, string> databaseObj in convertedGISObjects)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        throw new OperationCanceledException(cancellationToken);
      }
      // BAKE OBJECTS HERE
      // POC: QueuedTask
      var task = QueuedTask.Run(() =>
      {
        try
        {
          LayerFactory.Instance.CreateLayer(
            new Uri($"{databasePath}\\{databaseObj.Item2}"),
            _contextStack.Current.Document,
            layerName: databaseObj.Item1
          );
          onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / convertedGISObjects.Count);
        }
        catch (ArgumentException)
        {
          StandaloneTableFactory.Instance.CreateStandaloneTable(
            new Uri($"{databasePath}\\{databaseObj.Item2}"),
            _contextStack.Current.Document,
            tableName: databaseObj.Item1
          );
          onOperationProgressed?.Invoke("Adding to Map", (double)++bakeCount / convertedGISObjects.Count);
        }
      });
      task.Wait(cancellationToken);
    }

    return objectIds;
  }
}
