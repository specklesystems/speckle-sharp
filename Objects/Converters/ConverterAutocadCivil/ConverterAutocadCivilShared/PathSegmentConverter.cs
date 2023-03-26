using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Windows.Media;
using Polyline = Objects.Geometry.Polyline;
using System.Windows;
using Objects.Geometry;
using Curve = Autodesk.AutoCAD.DatabaseServices.Curve;
using Point = System.Windows.Point;
using System.Linq;
using System.Numerics;

namespace Objects.Converter.AutocadCivil
{
  internal class PathSegmentConverter
  {
    private Point StartPoint { get; set; }
    private Matrix3d MatrixMirrored { get; set; }

    private KnotCollection Knots { get; set; }
    private bool FigureClosed { get; set; }

    public PathSegmentConverter()
    {
      var plane = new Autodesk.AutoCAD.Geometry.Plane(new Point3d(0.0, 0.0, 0.0), new Vector3d(0.0, 1.0, 0.0));
      MatrixMirrored = Matrix3d.Mirroring(plane);

      Knots = new KnotCollection();
      Knots.Add(0.0);
      Knots.Add(0.0);
      Knots.Add(0.0);
      Knots.Add(1.0);
      Knots.Add(2.0);
      Knots.Add(3.0);
      Knots.Add(3.0);
      Knots.Add(3.0);
    }

    public List<Curve> ConverterPathFigureLetterToCurveCAD(PathFigure pathFigure, int pathFigureCount)
    {
      var segments = pathFigure.Segments;
      StartPoint = pathFigure.StartPoint;

      List<Curve> listCurve = new List<Curve>();

      foreach (PathSegment pathSegment in segments)
      {
        if (pathFigureCount == 1 && segments.Count == 1)
          FigureClosed = pathFigure.IsClosed;

        if (pathSegment is PolyBezierSegment)
          listCurve.AddRange(PolyBezierToSpline(pathSegment as PolyBezierSegment));
        else if (pathSegment is BezierSegment)
          listCurve.Add(BezierToSpline(pathSegment as BezierSegment));
        else if (pathSegment is PolyLineSegment)
          listCurve.Add(PolyLineSegmentToPolyline(pathSegment as PolyLineSegment));
        else if (pathSegment is LineSegment)
          listCurve.Add(LineSegmentToLine(pathSegment as LineSegment));
        else
        {
          throw new Exception($"Segment handling {pathSegment.GetType()} was not implemented to convert text to Speckle");
        }
      }

      //Coordinate system adjustments
      foreach (var curve in listCurve)
      {
        curve.TransformBy(MatrixMirrored);
      }

      return listCurve;
    }

    private List<Curve> PolyBezierToSpline(PolyBezierSegment polyBezierSegment)
    {
      PointCollection points = polyBezierSegment.Points;
      int count = points.Count / 3;

      List<Curve> listCurve = new List<Curve>();
      System.Windows.Point[] pointArray = new System.Windows.Point[4];
      for (int i = 0; i < count; i++)
      {
        pointArray[0] = StartPoint;
        pointArray[1] = points[(i * 3)];
        pointArray[2] = points[(i * 3) + 1];
        pointArray[3] = points[(i * 3) + 2];

        Curve curve = CreateBezierCurve(pointArray);
        if (curve != null)
          listCurve.Add(curve);

        StartPoint = pointArray[3];
      }

      return listCurve;
    }

    private Curve BezierToSpline(BezierSegment bezierSegment)
    {
      System.Windows.Point[] pointArray = new System.Windows.Point[4];
      pointArray[0] = StartPoint;
      pointArray[1] = bezierSegment.Point1;
      pointArray[2] = bezierSegment.Point2;
      pointArray[3] = bezierSegment.Point3;

      Curve curve = CreateBezierCurve(pointArray);

      StartPoint = pointArray[3];

      return curve;
    }

    private Curve CreateBezierCurve(System.Windows.Point[] pointArray) //Measure required for bizarre AutoCAD trait
    {
      Tolerance tolerance = new Tolerance();
      int degree = 3;
      Point3dCollection controlPoints = new Point3dCollection();

      for (int i = 0; i < 4; i++)
      {
        controlPoints.Add(new Point3d(pointArray[i].X, pointArray[i].Y, 0.0));
      }

      NurbCurve3d nc3d = new NurbCurve3d(degree, Knots, controlPoints, false);
      Double[] knotsArray = new Double[nc3d.Knots.Count];
      nc3d.Knots.CopyTo(knotsArray, 0);

      Autodesk.AutoCAD.Geometry.DoubleCollection dc = new Autodesk.AutoCAD.Geometry.DoubleCollection(knotsArray);
      NurbCurve3dData nc3dData = nc3d.DefinitionData;

      Spline spline = new Spline(nc3d.Degree, nc3d.IsRational, nc3d.IsClosed(), nc3d.IsPeriodic(out double per),
                  nc3dData.ControlPoints, dc, nc3dData.Weights, tolerance.EqualPoint, tolerance.EqualVector);

      spline.SetControlPointAt(0, controlPoints[0]);
      spline.SetControlPointAt(1, controlPoints[1]);
      spline.SetControlPointAt(2, controlPoints[2]);
      spline.SetControlPointAt(3, controlPoints[3]);

      if (spline.IsNull)
        return null;

      return spline;
    }

    private Curve PolyLineSegmentToPolyline(PolyLineSegment polyLineSegment)
    {
      PointCollection points = polyLineSegment.Points;
      int count = points.Count + 1;
      Autodesk.AutoCAD.DatabaseServices.Polyline polyline = new Autodesk.AutoCAD.DatabaseServices.Polyline(count);

      var point2d = new Point2d(StartPoint.X, StartPoint.Y);
      polyline.AddVertexAt(0, point2d, 0.0, 0.0, 0.0);
      for (int i = 1; i < count; i++)
      {
        System.Windows.Point point = points[i - 1];
        point2d = new Point2d(point.X, point.Y);
        polyline.AddVertexAt(i, point2d, 0.0, 0.0, 0.0);
      }

      polyline.Closed = FigureClosed;

      StartPoint = points.Last();

      return polyline;
    }

    private Curve LineSegmentToLine(LineSegment lineSegment)
    {
      Point3d stPoint = new Point3d(StartPoint.X, StartPoint.Y, 0.0);
      System.Windows.Point point = lineSegment.Point;
      Point3d endPoint = new Point3d(point.X, point.Y, 0.0);
      StartPoint = point;

      Autodesk.AutoCAD.DatabaseServices.Line line = new Autodesk.AutoCAD.DatabaseServices.Line(stPoint, endPoint);
      return line;
    }

  }
}
