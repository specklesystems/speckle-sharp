using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface INonNativeFeaturesUtils
{
  public List<(string parentPath, string converted)> WriteGeometriesToDatasets(
    Dictionary<string, (string parentPath, ACG.Geometry geom, string? parentId, Base baseObj)> convertedObjs,
    List<(
      bool isGisLayer,
      bool status,
      Base obj,
      string? datasetId,
      int? rowIndexNonGis,
      Exception? exception
    )> resultTracker
  );
}
