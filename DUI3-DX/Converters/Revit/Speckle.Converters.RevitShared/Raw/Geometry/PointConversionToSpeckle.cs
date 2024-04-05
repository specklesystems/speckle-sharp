using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PointConversionToSpeckle : IRawConversion<DB.Point, SOG.Point>
{
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;

  public PointConversionToSpeckle(IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter)
  {
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Point RawConvert(DB.Point target)
  {
    return _xyzToPointConverter.RawConvert(target.Coord);
  }
}
