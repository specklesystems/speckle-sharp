using Objects.Primitive;
using Speckle.Core.Models;
using Speckle.Core.Kits;

namespace Objects.Geometry
{
  public class Circle : Base, ICurve
  {
    public double? radius { get; set; }

    public Plane plane { get; set; }

    public Interval domain { get; set; }

    public Box bbox { get; set; }

    public Point center { get; set; }

    public double area { get; set; }

    public double length { get; set; }

    public Circle()
    {
    }

    public Circle(Plane plane, double radius, string units = Units.Meters, string applicationId = null)
    {
      this.plane = plane;
      this.radius = radius;
      this.applicationId = applicationId;
      this.units = units;
    }
  }
}
