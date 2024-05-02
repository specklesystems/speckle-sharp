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

namespace Speckle.Connectors.ArcGIS.Operations.Receive;

public class HostObjectBuilder : IHostObjectBuilder
{
  private readonly ISpeckleConverterToHost _toHostConverter;
  private readonly IArcGISProjectUtils _arcGISProjectUtils;

  // POC: figure out the correct scope to only initialize on Receive
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public HostObjectBuilder(
    ISpeckleConverterToHost toHostConverter,
    IArcGISProjectUtils arcGISProjectUtils,
    IConversionContextStack<Map, Unit> contextStack
  )
  {
    _toHostConverter = toHostConverter;
    _arcGISProjectUtils = arcGISProjectUtils;
    _contextStack = contextStack;
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
    IEnumerable<(List<string>, Base)> objectsWithPath = rootObject.TraverseWithPath((obj) => obj is not Collection);

    IEnumerable<(List<string>, Base)> gisObjectsWithPath = objectsWithPath.Where(
      x => x.Item2 is VectorLayer || x.Item2 is Objects.GIS.RasterLayer
    );
    IEnumerable<(List<string>, Base)> nonGisObjectsWithPath = objectsWithPath.Where(
      x => x.Item2 is not GisFeature && x.Item2 is not VectorLayer && x.Item2 is not Objects.GIS.RasterLayer
    );

    List<string> objectIds = new();
    int count = 0;
    foreach ((List<string> path, Base obj) in gisObjectsWithPath)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        throw new OperationCanceledException(cancellationToken);
      }

      try
      {
        // BAKE OBJECTS HERE

        // POC: QueuedTask
        QueuedTask.Run(() =>
        {
          try
          {
            string converted = (string)_toHostConverter.Convert(obj);
            objectIds.Add(obj.id);
            try
            {
              LayerFactory.Instance.CreateLayer(
                new Uri($"{databasePath}\\{converted}"),
                _contextStack.Current.Document,
                layerName: ((Collection)obj).name
              );
            }
            catch (ArgumentException)
            {
              StandaloneTableFactory.Instance.CreateStandaloneTable(
                new Uri($"{databasePath}\\{converted}"),
                _contextStack.Current.Document,
                tableName: ((Collection)obj).name
              );
            }
          }
          catch (ArgumentException)
          {
            // for the layers with "invalid" names
            // doesn't do anything actually, but needs to be logged
            throw;
          }
        });

        onOperationProgressed?.Invoke("Converting", (double)++count / objectsWithPath.Count());
      }
      catch (Exception e) when (!e.IsFatal()) // DO NOT CATCH SPECIFIC STUFF, conversion errors should be recoverable
      {
        // POC: report, etc.
        Debug.WriteLine("conversion error happened.");
      }
    }

    List<Geometry> convertedGeometries = new();

    return objectIds;
  }
}
