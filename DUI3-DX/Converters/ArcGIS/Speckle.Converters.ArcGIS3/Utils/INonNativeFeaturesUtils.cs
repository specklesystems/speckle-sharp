namespace Speckle.Converters.ArcGIS3.Utils;

public interface INonNativeFeaturesUtils
{
  public List<(string parentPath, string converted)> WriteGeometriesToDatasets(
    Dictionary<string, (string parentPath, ACG.Geometry geom, string? parentId)> convertedObjs
  );
}
