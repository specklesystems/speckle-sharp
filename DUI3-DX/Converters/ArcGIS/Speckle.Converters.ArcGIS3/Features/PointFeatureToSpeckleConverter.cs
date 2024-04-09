using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using MapPointFeature = ArcGIS.Core.Geometry.MapPoint;

namespace Speckle.Converters.ArcGIS3.Features;

public class PointFeatureToSpeckleConverter : IRawConversion<MapPointFeature, List<Base>>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PointFeatureToSpeckleConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public List<Base> RawConvert(MapPointFeature target)
  {
    if (
      GeometryEngine.Instance.Project(target, _contextStack.Current.Document.SpatialReference)
      is not MapPointFeature reprojectedPt
    )
    {
      throw new SpeckleConversionException(
        $"Conversion to Spatial Reference {_contextStack.Current.Document.SpatialReference} failed"
      );
    }
    List<Base> geometry =
      new() { new SOG.Point(reprojectedPt.X, reprojectedPt.Y, reprojectedPt.Z, _contextStack.Current.SpeckleUnits) };

    return geometry;
  }
}
