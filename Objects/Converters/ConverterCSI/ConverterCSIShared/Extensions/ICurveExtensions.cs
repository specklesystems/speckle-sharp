using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Objects;

namespace ConverterCSIShared.Extensions
{
  internal static class ICurveExtensions
  {
    public static IEnumerable<Point> ToPoints(this ICurve crv)
    {
      return crv switch
      {
        Line line => line.ToPoints(),
        Arc arc => arc.ToPoints(),
        Circle circle => throw new NotImplementedException("Circular openings are not yet supported"),
        Ellipse ellipse => throw new NotImplementedException("Ellipse openings are not yet supported"),
        Spiral spiral => throw new NotImplementedException("Spiral openings are not yet supported"),
        Curve curve => curve.ToPoints(),
        Polyline poly => poly.GetPoints(),
        Polycurve plc => plc.ToPoints(),
        _ => throw new Speckle.Core.Logging.SpeckleException("The provided geometry is not a valid curve"),
      };
    }
  }
}
