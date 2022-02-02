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
    }

    public sealed class Polyline : Base, ICurve
    {
      public List<PolylineSegment> polylineSegments { get; set; }
      public double length { get; set; }
      public Interval domain { get; set; }
    }

    public Polyline contourPolyline { get; set; }

    public List<Polyline> holePolylines { get; set; }
  }
}
