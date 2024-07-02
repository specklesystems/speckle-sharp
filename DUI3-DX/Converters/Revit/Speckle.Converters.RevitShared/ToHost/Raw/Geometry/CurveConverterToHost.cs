using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class CurveConverterToHost : ITypedConverter<SOG.Curve, DB.Curve>
{
  private readonly ITypedConverter<SOG.Point, DB.XYZ> _pointConverter;

  public CurveConverterToHost(ITypedConverter<SOG.Point, DB.XYZ> pointConverter)
  {
    _pointConverter = pointConverter;
  }

  public DB.Curve Convert(SOG.Curve target)
  {
    var pts = new List<DB.XYZ>();
    for (int i = 0; i < target.points.Count; i += 3)
    {
      //use PointToNative for conversion as that takes into account the Project Base Point
      var point = new SOG.Point(target.points[i], target.points[i + 1], target.points[i + 2], target.units);
      pts.Add(_pointConverter.Convert(point));
    }

    if (target.knots != null && target.weights != null && target.knots.Count > 0 && target.weights.Count > 0)
    {
      var weights = target.weights.GetRange(0, pts.Count);
      var speckleKnots = new List<double>(target.knots);
      if (speckleKnots.Count != pts.Count + target.degree + 1)
      {
        // Curve has rhino knots, repeat first and last.
        speckleKnots.Insert(0, speckleKnots[0]);
        speckleKnots.Add(speckleKnots[^1]);
      }

      //var knots = speckleKnots.GetRange(0, pts.Count + speckleCurve.degree + 1);
      var curve = DB.NurbSpline.CreateCurve(target.degree, speckleKnots, pts, weights);
      return curve;
    }
    else
    {
      var weights = target.weights.NotNull().GetRange(0, pts.Count);
      var curve = DB.NurbSpline.CreateCurve(pts, weights);
      return curve;
    }
  }
}
