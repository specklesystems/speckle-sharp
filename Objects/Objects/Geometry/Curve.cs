using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Geometry
{
  public class Curve : Base, ICurve, IHasBoundingBox, IHasArea
  {
    public int degree { get; set; }

    public bool periodic { get; set; }

    /// <summary>
    /// "True" if weights differ, "False" if weights are the same.
    /// </summary>
    public bool rational { get; set; }

    [DetachProperty]
    [Chunkable(31250)]
    public List<double> points { get; set; }

    /// <summary>
    /// Gets or sets the weights for this <see cref="Curve"/>. Use a default value of 1 for unweighted points.
    /// </summary>
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> weights { get; set; }

    /// <summary>
    /// Gets or sets the knots for this <see cref="Curve"/>. Count should be equal to <see cref="points"/> count + <see cref="degree"/> + 1.
    /// </summary>
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> knots { get; set; }

    public Interval domain { get; set; }

    public Polyline displayValue { get; set; }

    public bool closed { get; set; }

    public Box bbox { get; set; }

    public double area { get; set; }

    public double length { get; set; }

    public string units { get; set; }

    public Curve() { }

    public Curve(Polyline poly, string units = Units.Meters, string applicationId = null)
    {
      this.displayValue = poly;
      this.applicationId = applicationId;
      this.units = units;
    }

    public IEnumerable<Point> GetPoints()
    {
      if (points.Count % 3 != 0)throw new Speckle.Core.Logging.SpeckleException("Array malformed: length%3 != 0.");

      Point[ ] pts = new Point[points.Count / 3];
      var asArray = points.ToArray();
      for (int i = 2, k = 0; i < points.Count; i += 3)
        pts[k++] = new Point(asArray[i - 2], asArray[i - 1], asArray[i], units);
      return pts;
    }

    public List<double> ToList()
    {
      var list = new List<double>();
      var curve = this;
      list.Add(curve.degree); // 0
      list.Add(curve.periodic ? 1 : 0); // 1
      list.Add(curve.rational ? 1 : 0); // 2
      list.Add(curve.closed ? 1 : 0); // 3
      list.Add((double)curve.domain.start); // 4 
      list.Add((double)curve.domain.end); // 5

      list.Add(curve.points.Count); // 6
      list.Add(curve.weights.Count); // 7
      list.Add(curve.knots.Count); // 8

      list.AddRange(curve.points); // 9 onwards
      list.AddRange(curve.weights);
      list.AddRange(curve.knots);

      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.Curve);
      list.Insert(0, list.Count);
      return list;
    }

    public static Curve FromList(List<double> list)
    {
      if (list[0] != list.Count - 1) throw new Exception($"Incorrect length. Expected {list[0]}, got {list.Count}.");
      if (list[1] != CurveTypeEncoding.Curve) throw new Exception($"Wrong curve type. Expected {CurveTypeEncoding.Curve}, got {list[1]}.");

      var curve = new Curve();
      curve.degree = (int)list[2];
      curve.periodic = list[3] == 1;
      curve.rational = list[4] == 1;
      curve.closed = list[5] == 1;
      curve.domain = new Interval(list[6], list[7]);

      var pointsCount = (int)list[8];
      var weightsCount = (int)list[9];
      var knotsCount = (int)list[10];

      curve.points = list.GetRange(11, pointsCount);
      curve.weights = list.GetRange(11 + pointsCount, weightsCount);
      curve.knots = list.GetRange(11 + pointsCount + weightsCount, knotsCount);

      curve.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
      return curve;
    }
  }
}
