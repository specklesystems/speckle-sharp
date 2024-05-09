using Autodesk.AutoCAD.Geometry;
using Speckle.Converters.Autocad.Extensions;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Spline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class SplineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<ADB.Spline, SOG.Curve>
{
  private readonly IRawConversion<AG.Interval, SOP.Interval> _intervalConverter;
  private readonly IRawConversion<ADB.Extents3d, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;

  public SplineToSpeckleConverter(
    IRawConversion<AG.Interval, SOP.Interval> intervalConverter,
    IRawConversion<ADB.Extents3d, SOG.Box> boxConverter,
    IConversionContextStack<Document, ADB.UnitsValue> contextStack
  )
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((ADB.Spline)target);

  public SOG.Curve RawConvert(ADB.Spline target)
  {
    // get nurbs and geo data
    ADB.NurbsData data = target.NurbsData;

    // POC: HACK: check for incorrectly closed periodic curves (this seems like acad bug, has resulted from receiving rhino curves)
    bool periodicClosed = false;
    double length = 0;
    SOP.Interval domain = new();
    if (target.GetGeCurve() is NurbCurve3d nurbs)
    {
      length = nurbs.GetLength(nurbs.StartParameter, nurbs.EndParameter, 0.001);
      domain = _intervalConverter.RawConvert(nurbs.GetInterval());
      if (nurbs.Knots.Count < nurbs.NumberOfControlPoints + nurbs.Degree + 1 && target.IsPeriodic)
      {
        periodicClosed = true;
      }
    }

    // get points
    List<Point3d> points = new();
    foreach (Point3d point in data.GetControlPoints().OfType<Point3d>())
    {
      points.Add(point);
    }

    // NOTE: for closed periodic splines, autocad does not track last #degree points.
    // Add the first #degree control points to the list if so.
    if (periodicClosed)
    {
      points.AddRange(points.GetRange(0, target.Degree));
    }

    // get knots
    // NOTE: for closed periodic splines, autocad has #control points + 1 knots.
    // Add #degree extra knots to beginning and end with #degree - 1 multiplicity for first and last
    var knots = data.GetKnots().OfType<double>().ToList();
    if (periodicClosed)
    {
      double interval = knots[1] - knots[0]; //knot interval

      for (int i = 0; i < data.Degree; i++)
      {
        if (i < 2)
        {
          knots.Insert(knots.Count, knots[^1] + interval);
          knots.Insert(0, knots[0] - interval);
        }
        else
        {
          knots.Insert(knots.Count, knots[^1]);
          knots.Insert(0, knots[0]);
        }
      }
    }

    // get weights
    // NOTE: autocad assigns unweighted points a value of -1, and will return an empty list in the spline's nurbsdata if no points are weighted
    // NOTE: for closed periodic splines, autocad does not track last #degree points. Add the first #degree weights to the list if so.
    List<double> weights = new();
    for (int i = 0; i < target.NumControlPoints; i++)
    {
      double weight = target.WeightAt(i);
      weights.Add(weight <= 0 ? 1 : weight);
    }

    if (periodicClosed)
    {
      weights.AddRange(weights.GetRange(0, target.Degree));
    }

    // set nurbs curve info
    var curve = new SOG.Curve
    {
      points = points.SelectMany(o => o.ToArray()).ToList(),
      knots = knots,
      weights = weights,
      degree = target.Degree,
      periodic = target.IsPeriodic,
      rational = target.IsRational,
      closed = periodicClosed || target.Closed,
      length = length,
      domain = domain,
      bbox = _boxConverter.RawConvert(target.GeometricExtents),
      units = _contextStack.Current.SpeckleUnits
    };

    // POC: get display value if this is a database-resident spline
    // POC: if this is called by another converter that has created a spline, assumes the display value is set by that converter
    if (target.Database is not null)
    {
      curve.displayValue = GetDisplayValue(target);
    }

    return curve;
  }

  // POC: we might have DisplayValue converter/mapper?
  private SOG.Polyline GetDisplayValue(ADB.Spline spline)
  {
    ADB.Curve polySpline = spline.ToPolylineWithPrecision(10, false, false);
    List<double> verticesList = new();
    switch (polySpline)
    {
      case ADB.Polyline2d o:
        verticesList = o.GetSubEntities<ADB.Vertex2d>(
            ADB.OpenMode.ForRead,
            _contextStack.Current.Document.TransactionManager.TopTransaction
          )
          .Where(e => e.VertexType != ADB.Vertex2dType.SplineControlVertex) // POC: not validated yet!
          .SelectMany(o => o.Position.ToArray())
          .ToList();

        break;
      case ADB.Polyline3d o:
        verticesList = o.GetSubEntities<ADB.PolylineVertex3d>(
            ADB.OpenMode.ForRead,
            _contextStack.Current.Document.TransactionManager.TopTransaction
          )
          .Where(e => e.VertexType != ADB.Vertex3dType.ControlVertex)
          .SelectMany(o => o.Position.ToArray())
          .ToList();
        break;
    }

    return verticesList.ConvertToSpecklePolyline(_contextStack.Current.SpeckleUnits);
  }
}
