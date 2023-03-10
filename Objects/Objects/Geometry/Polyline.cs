using Speckle.Newtonsoft.Json;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Objects.Other;
using Speckle.Core.Logging;

namespace Objects.Geometry
{
  public class Polyline : Base, ICurve, IHasArea, IHasBoundingBox, IConvertible, ITransformable
  {
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> value { get; set; } = new List<double>();

    /// <summary>
    /// If true, do not add the last point to the value list. Polyline first and last points should be unique.
    /// </summary>
    public bool closed { get; set; }
    public Interval domain { get; set; }
    public Box bbox { get; set; }
    public double area { get; set; }
    public double length { get; set; }
    public string units { get; set; }

    public Polyline()
    { }
    
    [Obsolete("Use list constructor instead")]
    public Polyline(IEnumerable<double> coordinatesArray, string units = Units.Meters, string applicationId = null)
    : this(coordinatesArray.ToList(), units, applicationId)
    { }
    
    public Polyline(List<double> coordinates, string units = Units.Meters, string applicationId = null)
    {
      this.value = coordinates;
      this.units = units;
      this.applicationId = applicationId;
    }

    [JsonIgnore, Obsolete("Use " + nameof(GetPoints) + " Instead")]
    public List<Point> points => GetPoints();
    
    ///<remarks>This function may be suboptimal for performance for polylines with many points</remarks>
    /// <returns><see cref="value"/> as List of <see cref="Point"/>s</returns>
    /// <exception cref="SpeckleException">when list is malformed</exception>
    public List<Point> GetPoints()
    {
      if (value.Count % 3 != 0) throw new SpeckleException($"{nameof(Polyline)}.{nameof(value)} list is malformed: expected length to be multiple of 3");
      
      var pts = new List<Point>(value.Count / 3);
      for (int i = 2; i < value.Count; i += 3)
      {
        pts.Add(new Point(value[i - 2], value[i - 1], value[i], units));
      }
      return pts;
    }

    public List<double> ToList()
    {
      var list = new List<double>();
      list.Add(closed ? 1 : 0); // 2
      list.Add(domain.start ?? 0); // 3
      list.Add(domain.end ?? 1); // 4
      list.Add(value.Count); // 5
      list.AddRange(value); // 6 onwards

      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, CurveTypeEncoding.Polyline); // 1
      list.Insert(0, list.Count); // 0
      return list;
    }

    public static Polyline FromList(List<double> list)
    {
      var polyline = new Polyline();
      polyline.closed = list[2] == 1;
      polyline.domain = new Interval(list[3], list[4]);
      var pointCount = (int)list[5];
      polyline.value = list.GetRange(6, pointCount);
      polyline.units = Units.GetUnitFromEncoding(list[list.Count - 1]);
      return polyline;
    }

    object IConvertible.ToType(Type conversionType, IFormatProvider provider)
    {
      if (conversionType == typeof(Polycurve))
        return (Polycurve)this;
      throw new InvalidCastException();
    }

    public bool TransformTo(Transform transform, out ITransformable polyline)
    {
      // transform points
      var transformedPoints = new List<Point>();
      foreach (var point in GetPoints())
      {
        point.TransformTo(transform, out Point transformedPoint);
        transformedPoints.Add(transformedPoint);
      }

      polyline = new Polyline
      {
        value = transformedPoints.SelectMany(o => o.ToList()).ToList(),
        closed = closed,
        applicationId = applicationId,
        units = units
      };

      return true;
    }

    public TypeCode GetTypeCode()
    {
      throw new NotImplementedException();
    }

    public bool ToBoolean(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public byte ToByte(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public char ToChar(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public DateTime ToDateTime(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public decimal ToDecimal(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public double ToDouble(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public short ToInt16(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public int ToInt32(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public long ToInt64(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public sbyte ToSByte(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public float ToSingle(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public string ToString(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public object ToType(Type conversionType, IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public ushort ToUInt16(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public uint ToUInt32(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }

    public ulong ToUInt64(IFormatProvider provider)
    {
      throw new NotImplementedException();
    }
  }
}
