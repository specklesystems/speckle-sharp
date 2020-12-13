using Newtonsoft.Json;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Geometry
{
  public class Polyline : Base, ICurve, IHasArea, IHasBoundingBox
  {
    public List<double> value { get; set; } = new List<double>();
    public bool closed { get; set; }
    public Interval domain { get; set; }
    public Box bbox { get; set; }
    public double area { get; set; }
    public double length { get; set; }

    public Polyline()
    {

    }
    public Polyline(IEnumerable<double> coordinatesArray, string units = Units.Meters, string applicationId = null)
    {
      this.value = coordinatesArray.ToList();
      this.applicationId = applicationId;
      this.units = units;
    }

    [JsonIgnore]
    public List<Point> points
    {
      get
      {
        List<Point> points = new List<Point>();
        for (int i = 0; i < value.Count; i += 3)
        {
          points.Add(new Point(value[i], value[i + 1], value[i + 2], units));
        }
        return points;
      }
    }


    public static explicit operator Polycurve(Polyline polyline)
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
