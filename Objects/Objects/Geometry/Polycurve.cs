using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Geometry
{
  public class Polycurve : Base, ICurve, IHasArea, IHasBoundingBox
  {
    public List<ICurve> segments { get; set; } = new List<ICurve>();
    public Interval domain { get; set; }
    public bool closed { get; set; }
    public Box bbox { get; set; }
    public double area { get; set; }
    public double length { get; set; }

    public Polycurve()
    {
    }

    public Polycurve(string units = Units.Meters, string applicationId = null)
    {
      this.applicationId = applicationId;
      this.units = units;
    }

    public static implicit operator Polycurve(Polyline polyline)
    {
      Polycurve polycurve = new Polycurve
      {

        units = polyline.units,
        area = polyline.area,
        domain = polyline.domain,
        closed = polyline.closed,
        bbox = polyline.bbox,
        length = polyline.length
      };


      for (var i = 0; i < polyline.points.Count - 1; i++)
      {
        //close poly
        if (i == polyline.points.Count - 1 && polyline.closed)
        {
          var line = new Line(polyline.points[i], polyline.points[0], polyline.units);
          polycurve.segments.Add(line);
        }
        else
        {
          var line = new Line(polyline.points[i], polyline.points[i + 1], polyline.units);
          polycurve.segments.Add(line);
        }
      }

      return polycurve;
    }
  }
}
