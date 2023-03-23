using Objects.Geometry;
using Objects.Primitive;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects
{
  public static class CurveTypeEncoding
  {
    public const double Arc = 0;
    public const double Circle = 1;
    public const double Curve = 2;
    public const double Ellipse = 3;
    public const double Line = 4;
    public const double Polyline = 5;
    public const double PolyCurve = 6;
  }

  public static class CurveArrayEncodingExtensions
  {

    public static List<double> ToArray(List<ICurve> curves)
    {
      var list = new List<double>();
      foreach (var curve in curves)
      {
        switch (curve)
        {
          case Arc a: list.AddRange(a.ToList()); break;
          case Circle c: list.AddRange(c.ToList()); break;
          case Curve c: list.AddRange(c.ToList()); break;
          case Ellipse e: list.AddRange(e.ToList()); break;
          case Line l: list.AddRange(l.ToList()); break;
          case Polycurve p: list.AddRange(p.ToList()); break;
          case Polyline p: list.AddRange(p.ToList()); break;
          default: throw new Exception($"Unkown curve type: {curve.GetType()}.");
        }
      }

      return list;
    }

    public static List<ICurve> FromArray(List<double> list)
    {
      var curves = new List<ICurve>();
      if (list.Count == 0) return curves;
      var done = false;
      var currentIndex = 0;

      while (!done)
      {
        var itemLength = (int)list[currentIndex];
        var item = list.GetRange(currentIndex, itemLength + 1);

        switch (item[1])
        {
          case CurveTypeEncoding.Arc: curves.Add(Arc.FromList(item)); break;
          case CurveTypeEncoding.Circle: curves.Add(Circle.FromList(item)); break;
          case CurveTypeEncoding.Curve: curves.Add(Curve.FromList(item)); break;
          case CurveTypeEncoding.Ellipse: curves.Add(Ellipse.FromList(item)); break;
          case CurveTypeEncoding.Line: curves.Add(Line.FromList(item)); break;
          case CurveTypeEncoding.Polyline: curves.Add(Polyline.FromList(item)); break;
          case CurveTypeEncoding.PolyCurve: curves.Add(Polycurve.FromList(item)); break;
        }

        currentIndex += itemLength + 1;
        done = currentIndex >= list.Count;
      }
      return curves;
    }

  }
}