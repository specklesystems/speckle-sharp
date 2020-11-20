using Objects.Primitive;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Geometry
{
  public class Line : Base, ICurve
  {
    public List<double> value { get; set; }
    public Interval domain { get; set; }
    public Box boundingBox { get; set; }
    public Point center { get; set; }
    public double area { get; set; }
    public double length { get; set; }
    public string linearUnits { get; set; }

    public Line() { }

    public Line(double x, double y, double z = 0, string applicationId = null)
    {
      this.value = new List<double>() { x, y, z };
      this.applicationId = applicationId;
    }

    public Line(Point start, Point end, string applicationId = null)
    {
      this.value = start.value.Concat(end.value).ToList();
      this.applicationId = applicationId;
    }

    public Line(IEnumerable<double> coordinatesArray, string applicationId = null)
    {
      this.value = coordinatesArray.ToList();
      this.applicationId = applicationId;
    }
  }
}
