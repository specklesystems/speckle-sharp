using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad.ToSpeckle.Raw;

public class LineSegment3dToSpeckleRawConverter : ITypedConverter<AG.LineSegment3d, SOG.Line>
{
  private readonly ITypedConverter<AG.Point3d, SOG.Point> _pointConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public LineSegment3dToSpeckleRawConverter(
    ITypedConverter<AG.Point3d, SOG.Point> pointConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _pointConverter = pointConverter;
    _contextStack = contextStack;
  }

  public SOG.Line Convert(AG.LineSegment3d target) =>
    new(
      _pointConverter.Convert(target.StartPoint),
      _pointConverter.Convert(target.EndPoint),
      _contextStack.Current.SpeckleUnits
    )
    {
      length = target.Length,
      domain = new SOP.Interval(0, target.Length),
    };
}
