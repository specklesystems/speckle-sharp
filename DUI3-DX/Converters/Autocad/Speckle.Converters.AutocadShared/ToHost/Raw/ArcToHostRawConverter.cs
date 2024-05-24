using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Raw;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class ArcToHostRowConverter : ITypedConverter<SOG.Arc, AG.CircularArc3d>
{
  private readonly ITypedConverter<SOG.Point, AG.Point3d> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, AG.Vector3d> _vectorConverter;

  public ArcToHostRowConverter(
    ITypedConverter<SOG.Point, AG.Point3d> pointConverter,
    ITypedConverter<SOG.Vector, AG.Vector3d> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  public object Convert(Base target) => Convert((SOG.Arc)target);

  public AG.CircularArc3d Convert(SOG.Arc target)
  {
    AG.Point3d start = _pointConverter.Convert(target.startPoint);
    AG.Point3d end = _pointConverter.Convert(target.endPoint);
    AG.Point3d mid = _pointConverter.Convert(target.midPoint);
    AG.CircularArc3d arc = new(start, mid, end);

    AG.Vector3d normal = _vectorConverter.Convert(target.plane.normal);
    AG.Vector3d xdir = _vectorConverter.Convert(target.plane.xdir);
    arc.SetAxes(normal, xdir);

    if (target.startAngle is double startAngle && target.endAngle is double endAngle)
    {
      arc.SetAngles(startAngle, endAngle);
    }

    return arc;
  }
}
