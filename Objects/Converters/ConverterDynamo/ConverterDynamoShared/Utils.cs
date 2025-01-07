using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using Speckle.Core.Logging;
using DS = Autodesk.DesignScript.Geometry;

namespace Objects.Converter.Dynamo;

public static class Utils
{
  private const double EPS = 1e-6;
  private const string speckleKey = "speckle";

  #region Helper Methods



  public static DS.Point ToPoint(this double[] arr)
  {
    return DS.Point.ByCoordinates(arr[0], arr[1], arr[2]);
  }

  public static double ToDegrees(this double radians)
  {
    return radians * (180 / Math.PI);
  }

  public static double ToRadians(this double degrees)
  {
    return degrees * (Math.PI / 180);
  }

  public static bool Threshold(double value1, double value2, double error = EPS)
  {
    return Math.Abs(value1 - value2) <= error;
  }

  public static double Median(double min, double max)
  {
    return ((max - min) * 0.5) + min;
  }

  #endregion

  #region Curves Helper Methods




  public static bool IsLinear(this DS.Curve curve)
  {
    try
    {
      if (curve.IsClosed)
      {
        return false;
      }

      //Dynamo cannot be trusted when less than 1e-6
      var extremesDistance = curve.StartPoint.DistanceTo(curve.EndPoint);
      return Threshold(curve.Length, extremesDistance);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return false;
    }
  }

  public static DS.Line GetAsLine(this DS.Curve curve)
  {
    if (curve.IsClosed)
    {
      throw new ArgumentException("Curve is closed, cannot be a Line");
    }

    return DS.Line.ByStartPointEndPoint(curve.StartPoint, curve.EndPoint);
  }

  public static DS.Arc GetAsArc(this DS.Curve curve)
  {
    if (curve.IsClosed)
    {
      throw new ArgumentException("Curve is closed, cannot be an Arc");
    }

    using (DS.Point midPoint = curve.PointAtParameter(0.5))
    {
      return DS.Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint);
    }
  }

  public static bool IsCircle(this DS.Curve curve)
  {
    try
    {
      if (!curve.IsClosed)
      {
        return false;
      }

      using (DS.Point midPoint = curve.PointAtParameter(0.5))
      {
        double radius = curve.StartPoint.DistanceTo(midPoint) * 0.5;
        return Threshold(radius, (curve.Length) / (2 * Math.PI));
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return false;
    }
  }

  public static DS.Circle GetAsCircle(this DS.Curve curve)
  {
    if (!curve.IsClosed)
    {
      throw new ArgumentException("Curve is not closed, cannot be a Circle");
    }

    DS.Point start = curve.StartPoint;
    using (DS.Point midPoint = curve.PointAtParameter(0.5))
    using (
      DS.Point centre = DS.Point.ByCoordinates(
        Median(start.X, midPoint.X),
        Median(start.Y, midPoint.Y),
        Median(start.Z, midPoint.Z)
      )
    )
    {
      return DS.Circle.ByCenterPointRadiusNormal(centre, centre.DistanceTo(start), curve.Normal);
    }
  }

  public static bool IsEllipse(this DS.Curve curve)
  {
    try
    {
      if (!curve.IsClosed)
      {
        return false;
      }

      //http://www.numericana.com/answer/ellipse.htm
      double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
      DS.Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
      double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
      double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius
      points.ForEach(p => p.Dispose());

      double h = Math.Pow(a - b, 2) / Math.Pow(a + b, 2);
      double perimeter = Math.PI * (a + b) * (1 + (3 * h / (10 + Math.Sqrt(4 - 3 * h))));

      return Threshold(curve.Length, perimeter, 1e-5); //Ellipse perimeter is an approximation
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return false;
    }
  }

  public static DS.Ellipse GetAsEllipse(this DS.Curve curve)
  {
    if (!curve.IsClosed)
    {
      throw new ArgumentException("Curve is not closed, cannot be an Ellipse");
    }

    double[] parameters = new double[4] { 0, 0.25, 0.5, 0.75 };
    DS.Point[] points = parameters.Select(p => curve.PointAtParameter(p)).ToArray();
    double a = points[0].DistanceTo(points[2]) * 0.5; // Max Radius
    double b = points[1].DistanceTo(points[3]) * 0.5; // Min Radius

    using (
      DS.Point centre = DS.Point.ByCoordinates(
        Median(points[0].X, points[2].X),
        Median(points[0].Y, points[2].Y),
        Median(points[0].Z, points[2].Z)
      )
    )
    {
      points.ForEach(p => p.Dispose());

      return DS.Ellipse.ByPlaneRadii(
        DS.Plane.ByOriginNormalXAxis(centre, curve.Normal, DS.Vector.ByTwoPoints(centre, curve.StartPoint)),
        a,
        b
      );
    }
  }

  public static bool IsPolyline(this PolyCurve polycurve)
  {
    return polycurve.Curves().All(c => c.IsLinear());
  }

  public static bool IsArc(this DS.Curve curve)
  {
    try
    {
      if (curve.IsClosed)
      {
        return false;
      }

      using (DS.Point midPoint = curve.PointAtParameter(0.5))
      using (DS.Arc arc = DS.Arc.ByThreePoints(curve.StartPoint, midPoint, curve.EndPoint))
      {
        return Threshold(arc.Length, curve.Length);
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return false;
    }
  }

  #endregion

  public static Dictionary<string, object> GetSpeckleProperties(this DesignScriptEntity geometry)
  {
    var userData = geometry.Tags.LookupTag(speckleKey) as DesignScript.Builtin.Dictionary;
    if (userData == null)
    {
      return new Dictionary<string, object>();
    }

    return userData.ToSpeckleX();
    ;
  }

  public static T SetDynamoProperties<T>(this DesignScriptEntity geometry, Dictionary<string, object> properties)
  {
    if (properties != null)
    {
      geometry.Tags.AddTag(speckleKey, properties.ToNativeX());
    }

    return (T)Convert.ChangeType(geometry, typeof(T));
  }

  /// SpeckleCore does not currently support dictionaries, therefore avoiding the canonical ToSpeckle
  public static Dictionary<string, object> ToSpeckleX(this DesignScript.Builtin.Dictionary dict)
  {
    if (dict == null)
    {
      return null;
    }

    var speckleDict = new Dictionary<string, object>();
    foreach (var key in dict.Keys)
    {
      object value = dict.ValueAtKey(key);
      if (value is DesignScript.Builtin.Dictionary)
      {
        value = (value as DesignScript.Builtin.Dictionary).ToSpeckleX();
      }

      //TODO:
      //else if (value is Geometry)
      //{
      //  value = Converter.Serialise(value);
      //}
      speckleDict.Add(key, value);
    }

    return speckleDict;
  }

  /// SpeckleCore does not currently support dictionaries, therofere avoiding the canonical ToNative
  public static DesignScript.Builtin.Dictionary ToNativeX(this Dictionary<string, object> speckleDict)
  {
    if (speckleDict == null)
    {
      return null;
    }

    var keys = new List<string>();
    var values = new List<object>();
    foreach (var pair in speckleDict)
    {
      object value = pair.Value;
      if (value is Dictionary<string, object>)
      {
        value = (value as Dictionary<string, object>).ToNativeX();
      }

      //else if (value is Base)
      //{
      //  value = Converter.Deserialise(value as Base);
      //}
      keys.Add(pair.Key);
      values.Add(value);
    }

    return DesignScript.Builtin.Dictionary.ByKeysValues(keys, values);
  }
}
