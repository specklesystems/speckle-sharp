using Autodesk.Revit.DB;
using Objects.Primitive;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Line), 0)]
public class LineConversionToSpeckle : BaseConversionToSpeckle<DB.Line, SOG.Line>
{
  private readonly RevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ToSpeckleScalingService _scalingService;

  public LineConversionToSpeckle(
    RevitConversionContextStack contextStack,
    IRawConversion<XYZ, SOG.Point> xyzToPointConverter,
    ToSpeckleScalingService scalingService
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _scalingService = scalingService;
  }

  public override SOG.Line RawConvert(DB.Line target)
  {
    return new()
    {
      units = _contextStack.Current.SpeckleUnits,
      start = _xyzToPointConverter.RawConvert(target.GetEndPoint(0)),
      end = _xyzToPointConverter.RawConvert(target.GetEndPoint(1)),
      domain = new Interval(target.GetEndParameter(0), target.GetEndParameter(1)),
      length = _scalingService.ScaleLength(target.Length)
    };
  }
}
