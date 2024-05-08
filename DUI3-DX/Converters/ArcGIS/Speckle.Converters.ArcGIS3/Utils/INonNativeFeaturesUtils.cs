namespace Speckle.Converters.ArcGIS3.Utils;

public interface INonNativeFeaturesUtils
{
  public List<Tuple<string, string>> WriteGeometriesToDatasets(
    Dictionary<string, Tuple<List<string>, ACG.Geometry>> target
  );
}
