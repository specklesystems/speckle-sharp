using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Line, ADB.Line>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;

  public LineToHostConverter(ITypedConverter<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => Convert((SOG.Line)target);

  public ADB.Line Convert(SOG.Line target) =>
    new(_pointConverter.Convert(target.start), _pointConverter.Convert(target.end));
}
