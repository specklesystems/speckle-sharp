using ArcGIS.Core.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.ArcGIS3.ToSpeckle.Raw;

public class PointToSpeckleConverter : ITypedConverter<MapPoint, SOG.Point>
{
  private readonly IConversionContextStack<ArcGISDocument, Unit> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<ArcGISDocument, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public SOG.Point Convert(MapPoint target)
  {
    try
    {
      if (
        GeometryEngine.Instance.Project(target, _contextStack.Current.Document.Map.SpatialReference)
        is not MapPoint reprojectedPt
      )
      {
        throw new SpeckleConversionException(
          $"Conversion to Spatial Reference {_contextStack.Current.Document.Map.SpatialReference.Name} failed"
        );
      }
      return new(reprojectedPt.X, reprojectedPt.Y, reprojectedPt.Z, _contextStack.Current.SpeckleUnits);
    }
    catch (ArgumentException ex)
    {
      throw new SpeckleConversionException(
        $"Conversion to Spatial Reference {_contextStack.Current.Document.Map.SpatialReference} failed",
        ex
      );
    }
  }
}
