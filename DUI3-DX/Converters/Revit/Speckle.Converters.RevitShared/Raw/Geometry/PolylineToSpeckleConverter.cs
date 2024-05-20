using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PolylineToSpeckleConverter : ITypedConverter<DB.PolyLine, SOG.Polyline>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;

  public PolylineToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Polyline RawConvert(DB.PolyLine target)
  {
    var coords = target.GetCoordinates().SelectMany(coord => _xyzToPointConverter.RawConvert(coord).ToList()).ToList();
    return new SOG.Polyline(coords, _contextStack.Current.SpeckleUnits);
  }
}
