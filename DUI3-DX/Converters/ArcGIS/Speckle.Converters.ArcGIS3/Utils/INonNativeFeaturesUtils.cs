using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface INonNativeFeaturesUtils
{
  public List<(string parentPath, string converted)> WriteGeometriesToDatasets(
    Dictionary<TraversalContext, (string parentPath, ACG.Geometry geom)> convertedObjs
  );
}
