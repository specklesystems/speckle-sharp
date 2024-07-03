using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PointConversionToSpeckle : ITypedConverter<DB.Point, SOG.Point>
{
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;

  public PointConversionToSpeckle(ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter)
  {
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Point Convert(DB.Point target) => _xyzToPointConverter.Convert(target.Coord);
}
