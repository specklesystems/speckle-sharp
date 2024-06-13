using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PointConversionToSpeckle : ITypedConverter<IRevitPoint, SOG.Point>
{
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;

  public PointConversionToSpeckle(ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter)
  {
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Point Convert(IRevitPoint target) => _xyzToPointConverter.Convert(target.Coord);
}
