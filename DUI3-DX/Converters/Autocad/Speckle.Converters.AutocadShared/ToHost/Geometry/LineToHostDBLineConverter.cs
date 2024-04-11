using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Line), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class LineToHostDBLineConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Line, ADB.Line>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;

  public LineToHostDBLineConverter(IRawConversion<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Line)target);

  public ADB.Line RawConvert(SOG.Line target) =>
    new(_pointConverter.RawConvert(target.start), _pointConverter.RawConvert(target.end));
}
