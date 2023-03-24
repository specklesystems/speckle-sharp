using System.Collections.Generic;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad
{
  public sealed class ElementShape : Base
  {
    public sealed class PolylineSegment : Base, ICurve
    {
      public Point startPoint { get; set; }
      public Point endPoint { get; set; }
      public double arcAngle { get; set; }
      public double length { get; set; }
      public Interval domain { get; set; }

      public PolylineSegment() { }
      public PolylineSegment(Point startPoint, Point endPoint, double? arcAngle = null)
      {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
        this.arcAngle = arcAngle ?? 0;
      }
    }

    public sealed class Polyline : Base, ICurve
    {
      public List<PolylineSegment> polylineSegments { get; set; } = new List<PolylineSegment>();
      public double length { get; set; }
      public Interval domain { get; set; }

      public Polyline() { }

      public Polyline(List<PolylineSegment> segments)
      {
        polylineSegments = segments;
      }
    }

    public Polyline contourPolyline { get; set; }

    public List<Polyline> holePolylines { get; set; }

    public ElementShape() { }
    public ElementShape(Polyline contourPolyline, List<Polyline> holePolylines = null)
    {
      this.contourPolyline = contourPolyline;
      this.holePolylines = holePolylines;
    }
  }
}
