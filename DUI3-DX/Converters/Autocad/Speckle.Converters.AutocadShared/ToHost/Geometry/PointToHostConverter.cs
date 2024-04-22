using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostConverter : ISpeckleObjectToHostConversion, IRawConversion<SOG.Point, ADB.DBPoint>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;

  public PointToHostConverter(IRawConversion<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Point)target);

  public ADB.DBPoint RawConvert(SOG.Point target) => new(_pointConverter.RawConvert(target));
}
