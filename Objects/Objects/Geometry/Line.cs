using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Logging;

namespace Objects.Geometry
{
  public class Line : Base, ICurve, IHasBoundingBox
  {
    /// <summary>
    /// OBSOLETE - This is just here for backwards compatibility.
    /// You should not use this for anything. Access coordinates using start and end point.
    /// </summary>

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<double> value
    {
      get
      {
        return null;
      }
      set
      {
        start = new Point(value[0], value[1], value[2]);
        end = new Point(value[3], value[4], value[5]);
      } 
    }
    
    public Interval domain { get; set; }

    public Box bbox { get; set; }

    public double area { get; set; }
    public double length { get; set; }
    
    public Point start { get; set; }
    public Point end { get; set; }
    public Line() { }

    public Line(double x, double y, double z = 0, string units = Units.Meters, string applicationId = null)
    {
      this.start = new Point(x, y, z);
      this.end = null;
      this.applicationId = applicationId;
      this.units = units;
    }

    public Line(Point start, Point end, string units = Units.Meters, string applicationId = null)
    {
      this.start = start;
      this.end = end;
      this.applicationId = applicationId;
      this.units = units;
    }

    public Line(IEnumerable<double> coordinatesArray, string units = Units.Meters, string applicationId = null)
    {
      var enumerable = coordinatesArray.ToList();
      if (enumerable.Count < 6)
        throw new SpeckleException("Line from coordinate array requires 6 coordinates.");
      this.start = new Point(enumerable[0], enumerable[1], enumerable[2], units, applicationId);
      this.end = new Point(enumerable[3], enumerable[4], enumerable[5], units, applicationId);
      this.applicationId = applicationId;
      this.units = units;
    }

    public List<double> ToList() => start.ToList().Concat(end.ToList()).ToList();
  }
}
