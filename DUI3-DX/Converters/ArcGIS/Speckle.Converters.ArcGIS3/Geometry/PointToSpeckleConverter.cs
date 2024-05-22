using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

public class PointToSpeckleConverter : ITypedConverter<MapPoint, SOG.Point>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public SOG.Point Convert(MapPoint target)
  {
    try
    {
      if (
        GeometryEngine.Instance.Project(target, _contextStack.Current.Document.SpatialReference)
        is not MapPoint reprojectedPt
      )
      {
        throw new SpeckleConversionException(
          $"Conversion to Spatial Reference {_contextStack.Current.Document.SpatialReference} failed"
        );
      }
      return new(reprojectedPt.X, reprojectedPt.Y, reprojectedPt.Z, _contextStack.Current.SpeckleUnits);
    }
    catch (ArgumentException ex)
    {
      throw new SpeckleConversionException(
        $"Conversion to Spatial Reference {_contextStack.Current.Document.SpatialReference} failed",
        ex
      );
    }
  }
}
