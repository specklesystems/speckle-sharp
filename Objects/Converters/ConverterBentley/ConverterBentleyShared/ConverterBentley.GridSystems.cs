using System;
using System.Collections.Generic;
using System.Text;
using Bentley.DgnPlatformNET.Elements;
using Bentley.GeometryNET;
using Objects.BuiltElements;
using Objects.Geometry;
using Speckle.Core.Models;
#if (OPENBUILDINGS)
using Bentley.Building.Api;
#endif

namespace Objects.Converter.Bentley;

public partial class ConverterBentley
{
#if (OPENBUILDINGS)

  private static GridLine CreateGridLine(ICurve baseLine, string label, string units)
  {
    GridLine gridLine = new();
    gridLine.baseLine = baseLine;
    gridLine.label = label;
    gridLine.units = units;
    return gridLine;
  }

  public static ITFLoadableProject GetCurrentProject()
  {
    ITFApplication appInst = new TFApplicationList();

    if (0 == appInst.GetProject(0, out ITFLoadableProjectList projList) && projList != null)
    {
      ITFLoadableProject proj = projList.AsTFLoadableProject;
      return proj;
    }
    return null;
  }

  private static Point Rotate(Point point, double angle)
  {
    double sin = Math.Sin(angle);
    double cos = Math.Cos(angle);
    Point p = new(cos * point.x - sin * point.y, sin * point.x + cos * point.y);
    p.units = point.units;
    return p;
  }

  private static Vector Rotate(Vector vector, double angle)
  {
    double sin = Math.Sin(angle);
    double cos = Math.Cos(angle);
    Vector v = new(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y, vector.units);
    return v;
  }

  private static Point Translate(Point point, double deltaX, double deltaY)
  {
    Point p = new(point.x + deltaX, point.y + deltaY);
    p.units = point.units;
    return p;
  }

  private static Vector Translate(Vector vector, double deltaX, double deltaY)
  {
    Vector v = new(vector.x + deltaX, vector.y + deltaY, vector.units);
    return v;
  }

  public Base GridSystemsToSpeckle(ITFDrawingGrid drawingGrid, string units = null)
  {
    Base container = new();
    List<Base> gridLines = new();
    container["Grid Lines"] = gridLines;

    drawingGrid.GetGridSystems(0, out ITFGridSystemList gridSystems);
    if (gridSystems == null)
    {
      return null;
    }

    for (
      ITFGridSystem gridSystem = gridSystems.AsTFGridSystem;
      gridSystem != null;
      gridSystems.GetNext("", out gridSystems), gridSystem = (gridSystems != null) ? gridSystems.AsTFGridSystem : null
    )
    {
      gridSystem.GetGridCurves(0, out ITFGridCurveList curves);

      if (curves == null)
      {
        continue;
      }

      gridSystem.GetLCS(out DPoint3d origin, 0, out double angle);

      gridSystem.GetMinGridLineExtension(0, out double mindGridLineExtendsion);

      // find overall minimum/maximum extends of grid lines
      double minimumValueX = 0;
      double minimumValueY = 0;
      double maximumValueX = 0;
      double maximumValueY = 0;
      double maximumRadius = 0;
      double maximumCircularAngle = 0;

      List<ITFGridCurve> gridCurveList = new();
      for (
        ITFGridCurve gridCurve = curves.AsTFGridCurve;
        gridCurve != null;
        curves.GetNext("", out curves), gridCurve = curves != null ? curves.AsTFGridCurve : null
      )
      {
        gridCurveList.Add(gridCurve);
        gridCurve.GetType(0, out TFdGridCurveType curveType);
        gridCurve.GetValue(0, out double gridValue);
        gridCurve.GetMinimumValue(0, out double minimumValue);

        switch (curveType)
        {
          case (TFdGridCurveType.TFdGridCurveType_OrthogonalX):
            if (gridValue < minimumValueX)
            {
              minimumValueX = gridValue;
            }

            // grid lines pick up the minimum value of their neighbors
            if (minimumValue < minimumValueY)
            {
              minimumValueY = minimumValue;
            }

            if (gridValue > maximumValueX)
            {
              maximumValueX = gridValue;
            }

            break;

          case (TFdGridCurveType.TFdGridCurveType_OrthogonalY):
            if (gridValue < minimumValueY)
            {
              minimumValueY = gridValue;
            }

            // grid lines pick up the minimum value of their neighbors
            if (minimumValue < minimumValueX)
            {
              minimumValueX = minimumValue;
            }

            if (gridValue > maximumValueY)
            {
              maximumValueY = gridValue;
            }

            break;

          case (TFdGridCurveType.TFdGridCurveType_Circular):
            if (gridValue > maximumRadius)
            {
              maximumRadius = gridValue;
            }

            break;

          case (TFdGridCurveType.TFdGridCurveType_Radial):
            if (gridValue > maximumCircularAngle)
            {
              maximumCircularAngle = gridValue;
            }

            break;

          default:
            break;
        }
      }

      // for some reason only angles are scaled
      maximumCircularAngle *= UoR;

      foreach (ITFGridCurve gridCurve in gridCurveList)
      {
        ICurve baseLine;

        gridCurve.GetLabel(0, out string label);
        gridCurve.GetType(0, out TFdGridCurveType curveType);
        gridCurve.GetValue(0, out double gridValue);
        //gridCurve.GetMsElementDescrP(out Element obj, 0);

        var u = units ?? ModelUnits;

        //if (obj != null)
        //{
        //    // coordinate transformation
        //    //double axx = Math.Cos(angle);
        //    //double axy = -Math.Sin(angle);
        //    //double axz = 0;
        //    //double axw = origin.X;
        //    //double ayx = Math.Sin(angle);
        //    //double ayy = Math.Cos(angle);
        //    //double ayz = 0;
        //    //double ayw = origin.Y;
        //    //double azx = 0;
        //    //double azy = 0;
        //    //double azz = 1;
        //    //double azw = origin.Z;
        //    //DTransform3d transform = new DTransform3d(axx, axy, axz, axw, ayx, ayy, ayz, ayw, azx, azy, azz, azw);
        //    if (obj is LineElement)
        //    {
        //        Line line = LineToSpeckle((LineElement)obj, u);
        //        baseLine = TransformCurve(line, origin, angle);
        //    }
        //    else if (obj is ArcElement)
        //    {
        //        ICurve arc = ArcToSpeckle((ArcElement)obj, u);
        //        baseLine = arc;
        //        baseLine = TransformCurve(arc, origin, angle);
        //    }
        //    else
        //    {
        //        throw new NotSupportedException("GridCurveType " + curveType + " not supported!");
        //    }
        //    gridLines.Add(CreateGridLine(baseLine, label, u));
        //}
        switch (curveType)
        {
          case TFdGridCurveType.TFdGridCurveType_OrthogonalX:
          case TFdGridCurveType.TFdGridCurveType_OrthogonalY:
            baseLine = GridCurveToSpeckle(
              gridCurve,
              origin,
              angle,
              minimumValueX,
              minimumValueY,
              maximumValueX,
              maximumValueY,
              u
            );
            break;

          case TFdGridCurveType.TFdGridCurveType_Circular:
            Plane xy =
              new(
                new Point(origin.X, origin.Y, 0, u),
                new Vector(0, 0, 1, u),
                new Vector(1, 0, 0, u),
                new Vector(0, 1, 0, u),
                u
              );
            baseLine = new Arc(xy, gridValue, angle, maximumCircularAngle + angle, maximumCircularAngle, u);
            break;

          case TFdGridCurveType.TFdGridCurveType_Radial:
            Point startPoint = Translate(Rotate(new Point(0, 0, 0, u), angle), origin.X, origin.Y);
            Point endPoint = Translate(
              Rotate(Rotate(new Point(maximumRadius, 0, 0, u), gridValue * UoR), angle),
              origin.X,
              origin.Y
            );
            baseLine = new Line(startPoint, endPoint, u);
            break;

          default:
            continue;
        }
        gridLines.Add(CreateGridLine(baseLine, label, u));
      }
    }
    return container;
  }

  private ICurve TransformCurve(ICurve c, DPoint3d origin, double angle)
  {
    if (c is Line)
    {
      Line line = (Line)c;
      Point start = Translate(Rotate(line.start, angle), origin.X, origin.Y);
      Point end = Translate(Rotate(line.end, angle), origin.X, origin.Y);
      return new Line(start, end, line.units);
    }
    else if (c is Arc)
    {
      Arc arc = (Arc)c;
      Point startPoint = Translate(Rotate(arc.startPoint, angle), origin.X, origin.Y);
      Point midPoint = Translate(Rotate(arc.midPoint, angle), origin.X, origin.Y);
      Point endPoint = Translate(Rotate(arc.endPoint, angle), origin.X, origin.Y);
      Plane plane = TransformPlane(arc.plane, origin, angle);
      Arc transformed =
        new(
          plane,
          (double)arc.radius,
          (double)arc.startAngle + angle + Math.PI * 2,
          (double)arc.endAngle + angle + Math.PI * 2,
          (double)arc.angleRadians,
          arc.units
        );
      transformed.startPoint = startPoint;
      transformed.midPoint = midPoint;
      transformed.endPoint = endPoint;
      return transformed;
    }
    else if (c is Circle)
    {
      Circle circle = (Circle)c;
      Plane plane = TransformPlane(circle.plane, origin, angle);
      return new Circle(plane, (double)circle.radius, circle.units);
    }
    else
    {
      throw new NotSupportedException("ICurve " + c.GetType() + " not supported!");
    }
    return null;
  }

  private Plane TransformPlane(Plane plane, DPoint3d origin, double angle)
  {
    Point planeOrigin = Translate(plane.origin, origin.X, origin.Y);
    Vector xdir = Translate(Rotate(plane.xdir, angle), origin.X, origin.Y);
    Vector ydir = Translate(Rotate(plane.ydir, angle), origin.X, origin.Y);
    return new Plane(planeOrigin, plane.normal, xdir, ydir, plane.units);
  }

  public static ICurve GridCurveToSpeckle(
    ITFGridCurve gridCurve,
    DPoint3d origin,
    double angle,
    double minimumValueX = 0,
    double minimumValueY = 0,
    double maximumValueX = 0,
    double maximumValueY = 0,
    string units = null
  )
  {
    ICurve baseLine;

    gridCurve.GetType(0, out TFdGridCurveType curveType);
    gridCurve.GetMinimumValue(0, out double minimumValue);
    gridCurve.GetMaximumValue(0, out double maximumValue);
    gridCurve.GetValue(0, out double gridValue);

    Point startPoint,
      endPoint;
    switch (curveType)
    {
      case (TFdGridCurveType.TFdGridCurveType_OrthogonalX):
        if (minimumValue == 0)
        {
          minimumValue = minimumValueY;
        }

        if (maximumValue == 0)
        {
          maximumValue = maximumValueY;
        }

        startPoint = Translate(Rotate(new Point(gridValue, minimumValue, 0, units), angle), origin.X, origin.Y);
        endPoint = Translate(Rotate(new Point(gridValue, maximumValue, 0, units), angle), origin.X, origin.Y);
        baseLine = new Line(startPoint, endPoint, units);
        break;

      case (TFdGridCurveType.TFdGridCurveType_OrthogonalY):
        if (minimumValue == 0)
        {
          minimumValue = minimumValueX;
        }

        if (maximumValue == 0)
        {
          maximumValue = maximumValueX;
        }

        startPoint = Translate(Rotate(new Point(minimumValue, gridValue, 0, units), angle), origin.X, origin.Y);
        endPoint = Translate(Rotate(new Point(maximumValue, gridValue, 0, units), angle), origin.X, origin.Y);
        baseLine = new Line(startPoint, endPoint, units);
        break;

      default:
        throw new NotSupportedException("GridCurveType " + curveType + " not supported!");
    }
    return baseLine;
  }
#endif
}
