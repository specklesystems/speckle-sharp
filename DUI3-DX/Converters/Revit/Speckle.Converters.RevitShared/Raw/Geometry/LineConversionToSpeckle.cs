using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LineConversionToSpeckle : IRawConversion<DB.Line, SOG.Line>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ScalingServiceToSpeckle _scalingService;

  public LineConversionToSpeckle(
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter,
    ScalingServiceToSpeckle scalingService
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _scalingService = scalingService;
  }

  public SOG.Line RawConvert(DB.Line target) =>
    new()
    {
      units = _contextStack.Current.SpeckleUnits,
      start = _xyzToPointConverter.RawConvert(target.GetEndPoint(0)),
      end = _xyzToPointConverter.RawConvert(target.GetEndPoint(1)),
      domain = new Interval(target.GetEndParameter(0), target.GetEndParameter(1)),
      length = _scalingService.ScaleLength(target.Length)
    };
}
