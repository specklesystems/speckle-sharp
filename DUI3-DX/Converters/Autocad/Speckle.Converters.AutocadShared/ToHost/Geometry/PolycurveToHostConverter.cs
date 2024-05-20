using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.AutocadShared.ToHost.Geometry;

/// <summary>
/// A polycurve has segments as list and it can contain different kind of ICurve objects like Arc, Line, Polyline, Curve etc..
/// If polycurve segments are planar and only of type <see cref="SOG.Line"/> and <see cref="SOG.Arc"/>, it can be represented as Polyline in Autocad.
/// Otherwise we convert it as spline (list of ADB.Entity) that switch cases according to each segment type.
/// </summary>
[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostConverter : ISpeckleObjectToHostConversion
{
  private readonly ITypedConverter<SOG.Polycurve, ADB.Polyline> _polylineConverter;
  private readonly ITypedConverter<SOG.Polycurve, List<ADB.Entity>> _splineConverter;

  public PolycurveToHostConverter(
    ITypedConverter<SOG.Polycurve, ADB.Polyline> polylineConverter,
    ITypedConverter<SOG.Polycurve, List<ADB.Entity>> splineConverter
  )
  {
    _polylineConverter = polylineConverter;
    _splineConverter = splineConverter;
  }

  public object Convert(Base target)
  {
    SOG.Polycurve polycurve = (SOG.Polycurve)target;
    bool convertAsSpline = polycurve.segments.Any(s => s is not SOG.Line and not SOG.Arc);
    bool isPlanar = IsPolycurvePlanar(polycurve);

    if (convertAsSpline || !isPlanar)
    {
      return _splineConverter.RawConvert(polycurve);
    }
    else
    {
      return _polylineConverter.RawConvert(polycurve);
    }
  }

  private bool IsPolycurvePlanar(SOG.Polycurve polycurve)
  {
    double? z = null;
    foreach (Objects.ICurve segment in polycurve.segments)
    {
      switch (segment)
      {
        case SOG.Line o:
          z ??= o.start.z;
          if (o.start.z != z || o.end.z != z)
          {
            return false;
          }

          break;
        case SOG.Arc o:
          z ??= o.startPoint.z;
          if (o.startPoint.z != z || o.midPoint.z != z || o.endPoint.z != z)
          {
            return false;
          }

          break;
        case SOG.Curve o:
          z ??= o.points[2];
          for (int i = 2; i < o.points.Count; i += 3)
          {
            if (o.points[i] != z)
            {
              return false;
            }
          }

          break;
        case SOG.Spiral o:
          z ??= o.startPoint.z;
          if (o.startPoint.z != z || o.endPoint.z != z)
          {
            return false;
          }

          break;
      }
    }
    return true;
  }
}
