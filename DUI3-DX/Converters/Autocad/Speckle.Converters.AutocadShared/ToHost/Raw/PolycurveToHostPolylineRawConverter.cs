using System;
using System.Linq;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.AutocadShared.ToHost.Raw;

public class PolycurveToHostPolylineRawConverter : IRawConversion<SOG.Polycurve, ADB.Polyline>
{
  private readonly IConversionContextStack<Document, ADB.UnitsValue> _contextStack;
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;

  public PolycurveToHostPolylineRawConverter(
    IConversionContextStack<Document, ADB.UnitsValue> contextStack,
    IRawConversion<SOG.Point, AG.Point3d> pointConverter
  )
  {
    _contextStack = contextStack;
    _pointConverter = pointConverter;
  }

  public ADB.Polyline RawConvert(SOG.Polycurve target)
  {
    ADB.Polyline polyline = new() { Closed = target.closed };
    var plane = new AG.Plane(
      AG.Point3d.Origin,
      AG.Vector3d.ZAxis.TransformBy(_contextStack.Current.Document.Editor.CurrentUserCoordinateSystem)
    );

    int count = 0;
    foreach (var segment in target.segments)
    {
      switch (segment)
      {
        case SOG.Line o:
          polyline.AddVertexAt(count, _pointConverter.RawConvert(o.start).Convert2d(plane), 0, 0, 0);
          if (!target.closed && count == target.segments.Count - 1)
          {
            polyline.AddVertexAt(count + 1, _pointConverter.RawConvert(o.end).Convert2d(plane), 0, 0, 0);
          }

          count++;
          break;
        case SOG.Arc arc:
          // POC: possibly endAngle and startAngle null?
          var angle = arc.endAngle - arc.startAngle;
          angle = angle < 0 ? angle + 2 * Math.PI : angle;
          var bulge = Math.Tan((double)angle / 4) * BulgeDirection(arc.startPoint, arc.midPoint, arc.endPoint);
          polyline.AddVertexAt(count, _pointConverter.RawConvert(arc.startPoint).Convert2d(plane), bulge, 0, 0);
          if (!target.closed && count == target.segments.Count - 1)
          {
            polyline.AddVertexAt(count + 1, _pointConverter.RawConvert(arc.endPoint).Convert2d(plane), 0, 0, 0);
          }

          count++;
          break;
        case SOG.Spiral o:
          var vertices = o.displayValue.GetPoints().Select(_pointConverter.RawConvert).ToList();
          foreach (var vertex in vertices)
          {
            polyline.AddVertexAt(count, vertex.Convert2d(plane), 0, 0, 0);
            count++;
          }
          break;
        default:
          break;
      }
    }

    return polyline;
  }

  // calculates bulge direction: (-) clockwise, (+) counterclockwise
  private int BulgeDirection(SOG.Point start, SOG.Point mid, SOG.Point end)
  {
    // get vectors from points
    double[] v1 = new double[] { end.x - start.x, end.y - start.y, end.z - start.z }; // vector from start to end point
    double[] v2 = new double[] { mid.x - start.x, mid.y - start.y, mid.z - start.z }; // vector from start to mid point

    // calculate cross product z direction
    double z = v1[0] * v2[1] - v2[0] * v1[1];

    if (z > 0)
    {
      return -1;
    }
    else
    {
      return 1;
    }
  }
}
