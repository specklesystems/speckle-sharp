using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;

namespace Speckle.Converters.ArcGIS3.Features;

[NameAndRankValue(nameof(ACG.MapPoint), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointFeatureToSpeckleConverter : IToSpeckleTopLevelConverter, ITypedConverter<ACG.MapPoint, Base>
{
  private readonly IConversionContextStack<Map, ACG.Unit> _contextStack;

  public PointFeatureToSpeckleConverter(IConversionContextStack<Map, ACG.Unit> contextStack)
  {
    _contextStack = contextStack;
  }

  public Base Convert(object target) => Convert((ACG.MapPoint)target);

  public Base Convert(ACG.MapPoint target)
  {
    if (
      ACG.GeometryEngine.Instance.Project(target, _contextStack.Current.Document.SpatialReference)
      is not ACG.MapPoint reprojectedPt
    )
    {
      throw new SpeckleConversionException(
        $"Conversion to Spatial Reference {_contextStack.Current.Document.SpatialReference} failed"
      );
    }
    List<Base> geometry =
      new() { new SOG.Point(reprojectedPt.X, reprojectedPt.Y, reprojectedPt.Z, _contextStack.Current.SpeckleUnits) };

    return geometry[0];
  }
}
