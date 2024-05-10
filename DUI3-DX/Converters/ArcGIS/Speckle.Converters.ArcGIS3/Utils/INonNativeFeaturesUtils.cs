namespace Speckle.Converters.ArcGIS3.Utils;

public interface INonNativeFeaturesUtils
{
  public List<(string, string)> WriteGeometriesToDatasets(
    Dictionary<string, (string, ACG.Geometry, string?)> convertedObjs
  );
}
