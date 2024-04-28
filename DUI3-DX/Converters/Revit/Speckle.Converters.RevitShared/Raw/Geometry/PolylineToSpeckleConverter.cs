using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PolylineToSpeckleConverter : IRawConversion<DB.PolyLine, SOG.Polyline>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;

  public PolylineToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Polyline RawConvert(DB.PolyLine polyline)
  {
    var coords = polyline
      .GetCoordinates()
      .SelectMany(coord => _xyzToPointConverter.RawConvert(coord).ToList())
      .ToList();
    return new SOG.Polyline(coords, _contextStack.Current.SpeckleUnits);
  }
}
