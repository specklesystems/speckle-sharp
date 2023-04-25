using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Other;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry;

public class Line : Base, ICurve, IHasBoundingBox, ITransformable<Line>
{
  public Line() { }

  public Line(double x, double y, double z = 0, string units = Units.Meters, string applicationId = null)
  {
    start = new Point(x, y, z);
    end = null;
    this.applicationId = applicationId;
    this.units = units;
  }

  public Line(Point start, Point end, string units = Units.Meters, string applicationId = null)
  {
    this.start = start;
    this.end = end;
    length = Point.Distance(start, end);
    this.applicationId = applicationId;
    this.units = units;
  }

  public Line(IList<double> coordinates, string units = Units.Meters, string applicationId = null)
  {
    if (coordinates.Count < 6)
      throw new SpeckleException("Line from coordinate array requires 6 coordinates.");
    start = new Point(coordinates[0], coordinates[1], coordinates[2], units, applicationId);
    end = new Point(coordinates[3], coordinates[4], coordinates[5], units, applicationId);
    length = Point.Distance(start, end);
    this.applicationId = applicationId;
    this.units = units;
  }

  [Obsolete("Use IList constructor")]
  public Line(IEnumerable<double> coordinatesArray, string units = Units.Meters, string applicationId = null)
    : this(coordinatesArray.ToList(), units, applicationId) { }

  /// <summary>
  /// OBSOLETE - This is just here for backwards compatibility.
  /// You should not use this for anything. Access coordinates using start and end point.
  /// </summary>

  [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
  public List<double> value
  {
    get => null;
    set
    {
      if (value == null)
        return;
      start = new Point(value[0], value[1], value[2]);
      end = new Point(value[3], value[4], value[5]);
    }
  }

  public double area { get; set; }

  public string units { get; set; }

  public Point start { get; set; }
  public Point end { get; set; }

  public Interval domain { get; set; }
  public double length { get; set; }

  public Box bbox { get; set; }

  public bool TransformTo(Transform transform, out Line transformed)
  {
    start.TransformTo(transform, out Point transformedStart);
    end.TransformTo(transform, out Point transformedEnd);
    transformed = new Line
    {
      start = transformedStart,
      end = transformedEnd,
      applicationId = applicationId,
      units = units,
      domain = domain == null ? null : new Interval { start = domain.start, end = domain.end }
    };
    return true;
  }

  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    var res = TransformTo(transform, out Line line);
    transformed = line;
    return res;
  }

  public List<double> ToList()
  {
    var list = new List<double>();
    list.AddRange(start.ToList());
    list.AddRange(end.ToList());
    list.Add(domain.start ?? 0);
    list.Add(domain.end ?? 1);
    list.Add(Units.GetEncodingFromUnit(units));
    list.Insert(0, CurveTypeEncoding.Line);
    list.Insert(0, list.Count);
    return list;
  }

  public static Line FromList(List<double> list)
  {
    var units = Units.GetUnitFromEncoding(list[list.Count - 1]);
    var startPt = new Point(list[2], list[3], list[4], units);
    var endPt = new Point(list[5], list[6], list[7], units);
    var line = new Line(startPt, endPt, units);
    line.domain = new Interval(list[8], list[9]);
    return line;
  }
}
