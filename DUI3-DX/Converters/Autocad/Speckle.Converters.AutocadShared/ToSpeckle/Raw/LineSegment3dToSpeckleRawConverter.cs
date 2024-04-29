using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class LineSegment3dToSpeckleRawConverter : IRawConversion<AG.LineSegment3d, SOG.Line>
{
  private readonly IRawConversion<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public LineSegment3dToSpeckleRawConverter(
    IRawConversion<AG.Point3d, SOG.Point> pointConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public SOG.Line RawConvert(AG.LineSegment3d target) =>
    new(
      _pointConverter.RawConvert(target.StartPoint),
      _pointConverter.RawConvert(target.EndPoint),
      _contextStack.Current.SpeckleUnits
    )
    {
      length = target.Length,
      domain = new SOP.Interval(0, target.Length),
    };
}
