using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Geometry
{
  public class Curve : Base, ICurve, IHasBoundingBox, IHasArea
  {
    public int degree { get; set; }

    public bool periodic { get; set; }

    public bool rational { get; set; }

    [DetachProperty]
    [Chunkable(20000)]
    public List<double> points { get; set; }
    
    [DetachProperty]
    [Chunkable(20000)]
    public List<double> weights { get; set; }

    /// <summary>
    /// Gets or sets the knots for this <see cref="Curve"/>. Count should be equal to <see cref="points"/> count + <see cref="degree"/> + 1.
    /// </summary>
    [DetachProperty]
    [Chunkable(20000)]
    public List<double> knots { get; set; }

    public Interval domain { get; set; }

    
    public Polyline displayValue { get; set; }

    public bool closed { get; set; }

    public Box bbox { get; set; }

    public double area { get; set; }

    public double length { get; set; }

    public Curve() { }

    public Curve(Polyline poly, string units = Units.Meters, string applicationId = null)
    {
      this.displayValue = poly;
      this.applicationId = applicationId;
      this.units = units;
    }

    public IEnumerable<Point> GetPoints()
    {
      if (points.Count % 3 != 0) throw new Exception("Array malformed: length%3 != 0.");

      Point[] pts = new Point[points.Count / 3];
      var asArray = points.ToArray();
      for (int i = 2, k = 0; i < points.Count; i += 3)
        pts[k++] = new Point(asArray[i - 2], asArray[i - 1], asArray[i], units);
      return pts;
    }
  }
}
