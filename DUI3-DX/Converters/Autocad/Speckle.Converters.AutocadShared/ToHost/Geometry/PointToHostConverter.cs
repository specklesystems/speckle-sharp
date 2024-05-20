using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Point, ADB.DBPoint>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;

  public PointToHostConverter(ITypedConverter<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => Convert((SOG.Point)target);

  public ADB.DBPoint Convert(SOG.Point target) => new(_pointConverter.Convert(target));
}
