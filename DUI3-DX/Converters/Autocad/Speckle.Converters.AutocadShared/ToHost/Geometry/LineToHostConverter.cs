using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Line, ADB.Line>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;

  public LineToHostConverter(ITypedConverter<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Line)target);

  public ADB.Line RawConvert(SOG.Line target) =>
    new(_pointConverter.RawConvert(target.start), _pointConverter.RawConvert(target.end));
}
