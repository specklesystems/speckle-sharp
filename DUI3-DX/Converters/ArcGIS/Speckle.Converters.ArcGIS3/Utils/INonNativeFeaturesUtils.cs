using Speckle.Core.Models.GraphTraversal;

namespace Speckle.Converters.ArcGIS3.Utils;

public interface INonNativeFeaturesUtils
{
  public void WriteGeometriesToDatasets(Dictionary<TraversalContext, ObjectConversionTracker> conversionTracker);
}
