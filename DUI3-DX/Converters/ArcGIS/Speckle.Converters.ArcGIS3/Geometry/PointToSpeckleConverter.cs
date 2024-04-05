using ArcGIS.Core.Geometry;
using ArcMapPoint = ArcGIS.Core.Geometry.MapPoint;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Geometry;

[NameAndRankValue(nameof(ArcMapPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<ArcMapPoint, SOG.Point>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;

  public PointToSpeckleConverter(IConversionContextStack<Map, Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ArcMapPoint)target);

  public SOG.Point RawConvert(ArcMapPoint target)
  {
    if (
      GeometryEngine.Instance.Project(target, _contextStack.Current.Document.SpatialReference)
      is not ArcMapPoint reprojectedPt
    )
    {
      throw new SpeckleConversionException(
        $"Conversion to Spatial Reference {_contextStack.Current.Document.SpatialReference} failed"
      );
    }
    return new(reprojectedPt.X, reprojectedPt.Y, reprojectedPt.Z, _contextStack.Current.SpeckleUnits);
  }
}
