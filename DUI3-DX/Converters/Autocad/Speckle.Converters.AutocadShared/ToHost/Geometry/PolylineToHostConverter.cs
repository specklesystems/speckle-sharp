using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToHostConverter : ISpeckleObjectToHostConversion, ITypedConverter<SOG.Polyline, ADB.Polyline3d>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;

  public PolylineToHostConverter(ITypedConverter<SOG.Point, AG.Point3d> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Polyline)target);

  public ADB.Polyline3d RawConvert(SOG.Polyline target)
  {
    AG.Point3dCollection vertices = new();
    target.GetPoints().ForEach(o => vertices.Add(_pointConverter.RawConvert(o)));
    return new(ADB.Poly3dType.SimplePoly, vertices, target.closed);
  }
}
