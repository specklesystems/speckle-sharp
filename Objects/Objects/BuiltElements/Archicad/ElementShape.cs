using System.Collections.Generic;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Archicad;

public sealed class ElementShape : Base
{
  public ElementShape() { }

  public ElementShape(Polyline contourPolyline, List<Polyline> holePolylines = null)
  {
    this.contourPolyline = contourPolyline;
    this.holePolylines = holePolylines;
  }

  public Polyline contourPolyline { get; set; }

  public List<Polyline> holePolylines { get; set; }

  public sealed class PolylineSegment : Base, ICurve
  {
    public PolylineSegment() { }

    public PolylineSegment(Point startPoint, Point endPoint, double? arcAngle = null, bool? bodyFlag = null)
    {
      this.startPoint = startPoint;
      this.endPoint = endPoint;
      this.arcAngle = arcAngle ?? 0;
      this.bodyFlag = bodyFlag;
    }

    public Point startPoint { get; set; }
    public Point endPoint { get; set; }
    public double arcAngle { get; set; }
    public bool? bodyFlag { get; set; }
    public double length { get; set; }
    public Interval domain { get; set; }
  }

  public sealed class Polyline : Base, ICurve
  {
    public Polyline() { }

    public Polyline(List<PolylineSegment> segments)
    {
      polylineSegments = segments;
    }

    public List<PolylineSegment> polylineSegments { get; set; } = new();
    public double length { get; set; }
    public Interval domain { get; set; }
  }
}
