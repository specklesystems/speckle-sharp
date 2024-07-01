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
      // reproject to Active CRS
      if (
        GeometryEngine.Instance.Project(target, _contextStack.Current.Document.ActiveCRSoffsetRotation.SpatialReference)
        is not MapPoint reprojectedPt
      )
      {
        throw new SpeckleConversionException(
          $"Conversion to Spatial Reference {_contextStack.Current.Document.ActiveCRSoffsetRotation.SpatialReference.Name} failed"
        );
      }

      // convert to Speckle Pt
      SOG.Point reprojectedSpecklePt =
        new(
          reprojectedPt.X,
          reprojectedPt.Y,
          reprojectedPt.Z,
          _contextStack.Current.Document.ActiveCRSoffsetRotation.SpeckleUnitString
        );
      SOG.Point scaledMovedRotatedPoint = _contextStack.Current.Document.ActiveCRSoffsetRotation.OffsetRotateOnSend(
        reprojectedSpecklePt
      );
      return scaledMovedRotatedPoint;
    }
    catch (ArgumentException ex)
    {
      throw new SpeckleConversionException(
        $"Conversion to Spatial Reference {_contextStack.Current.Document.ActiveCRSoffsetRotation.SpatialReference.Name} failed",
        ex
      );
    }
  }
}
