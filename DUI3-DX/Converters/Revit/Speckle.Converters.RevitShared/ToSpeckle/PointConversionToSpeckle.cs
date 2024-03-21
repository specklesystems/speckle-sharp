using Speckle.Converters.Common;
using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Point), 0)]
public class PointConversionToSpeckle : BaseConversionToSpeckle<DB.Point, SOG.Point>
{
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;

  public PointConversionToSpeckle(IRawConversion<XYZ, SOG.Point> xyzToPointConverter)
  {
    _xyzToPointConverter = xyzToPointConverter;
  }

  public override SOG.Point RawConvert(Point target)
  {
    return _xyzToPointConverter.RawConvert(target.Coord);
  }
}
