using System.Collections.Generic;
using System.Linq;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.AutocadShared.ToHost.Geometry;

[NameAndRankValue(nameof(SOG.Polycurve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolycurveToHostDBPolycurveConverter : ISpeckleObjectToHostConversion
{
  private readonly IRawConversion<SOG.Line, ADB.Line> _lineConverter;
  private readonly IRawConversion<SOG.Arc, ADB.Arc> _arcConverter;
  private readonly IRawConversion<SOG.Polycurve, ADB.Polyline> _polylineConverter;

  public PolycurveToHostDBPolycurveConverter(
    IRawConversion<SOG.Line, ADB.Line> lineConverter,
    IRawConversion<SOG.Arc, ADB.Arc> arcConverter,
    IRawConversion<SOG.Polycurve, ADB.Polyline> polylineConverter
    )
  {
    _lineConverter = lineConverter;
    _arcConverter = arcConverter;
    _polylineConverter = polylineConverter;
  }

  public object Convert(Base target)
  {
    SOG.Polycurve polycurve = target as SOG.Polycurve;
    bool convertAsSpline = polycurve.segments.Any(s => s is not SOG.Line and not SOG.Arc);
    bool isPlanar = IsPolycurvePlanar(polycurve);

    if (convertAsSpline || !isPlanar)
    {
      return null; // POC: handle polycurve as spline here
    }
    else
    {
      return _polylineConverter.RawConvert(polycurve);
    }
  }

  private bool IsPolycurvePlanar(SOG.Polycurve polycurve)
  {
    double? z = null;
    foreach (var segment in polycurve.segments)
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
