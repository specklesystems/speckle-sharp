using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Raw;

[NameAndRankValue(nameof(SOG.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class ArcToHostRowConverter : IRawConversion<SOG.Arc, AG.CircularArc3d>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;

  public ArcToHostRowConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Arc)target);

  public AG.CircularArc3d RawConvert(SOG.Arc target)
  {
    Point3d start = _pointConverter.RawConvert(target.startPoint);
    Point3d end = _pointConverter.RawConvert(target.endPoint);
    Point3d mid = _pointConverter.RawConvert(target.midPoint);
    CircularArc3d arc = new(start, mid, end);

    AG.Vector3d normal = _vectorConverter.RawConvert(target.plane.normal);
    AG.Vector3d xdir = _vectorConverter.RawConvert(target.plane.xdir);
    arc.SetAxes(normal, xdir);

    if (target.startAngle is double startAngle && target.endAngle is double endAngle)
    {
      arc.SetAngles(startAngle, endAngle);
    }

    return arc;
  }
}
