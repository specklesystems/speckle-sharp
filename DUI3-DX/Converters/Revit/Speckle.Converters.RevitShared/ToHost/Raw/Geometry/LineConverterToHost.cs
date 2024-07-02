using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class LineConverterToHost : ITypedConverter<SOG.Line, DB.Line>
{
  private readonly ITypedConverter<SOG.Point, DB.XYZ> _pointToXyzConverter;

  public LineConverterToHost(ITypedConverter<SOG.Point, DB.XYZ> pointToXyzConverter)
  {
    _pointToXyzConverter = pointToXyzConverter;
  }

  public DB.Line Convert(SOG.Line target) =>
    DB.Line.CreateBound(_pointToXyzConverter.Convert(target.start), _pointToXyzConverter.Convert(target.end));
}
