#if ADVANCESTEEL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.Geometry;
using AcadGeo = Autodesk.AutoCAD.Geometry;

using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Vector = Objects.Geometry.Vector;

using MathNet.Spatial.Euclidean;

using ASPolyline3d = Autodesk.AdvanceSteel.Geometry.Polyline3d;
using ASCurve3d = Autodesk.AdvanceSteel.Geometry.Curve3d;
using ASLineSeg3d = Autodesk.AdvanceSteel.Geometry.LineSeg3d;
using ASCircArc3d = Autodesk.AdvanceSteel.Geometry.CircArc3d;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using ASVector3d = Autodesk.AdvanceSteel.Geometry.Vector3d;
using ASExtents = Autodesk.AdvanceSteel.Geometry.Extents;
using ASPlane = Autodesk.AdvanceSteel.Geometry.Plane;
using ASBoundBlock3d = Autodesk.AdvanceSteel.Geometry.BoundBlock3d;

using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;
using Autodesk.AdvanceSteel.DocumentManagement;
using Autodesk.AdvanceSteel.DotNetRoots.Units;
using Autodesk.AutoCAD.PlottingServices;
using Speckle.Newtonsoft.Json.Linq;

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  private Point PointToSpeckle(ASPoint3d point, string units = null)
  {
    //TODO: handle units.none?
    var u = units ?? ModelUnits;
    var extPt = ToExternalCoordinates(PointASToAcad(point));
    return new Point(extPt.X, extPt.Y, extPt.Z, u);
  }

  private Point3d PointASToAcad(ASPoint3d point)
  {
    return new Point3d(point.x * FactorFromNative, point.y * FactorFromNative, point.z * FactorFromNative);
  }

  private Point3D PointToMath(ASPoint3d point)
  {
    return new Point3D(point.x * FactorFromNative, point.y * FactorFromNative, point.z * FactorFromNative);
  }

  public Vector VectorToSpeckle(ASVector3d vector, string units = null)
  {
    var u = units ?? ModelUnits;
    var extV = ToExternalCoordinates(VectorASToAcad(vector));
    return new Vector(extV.X, extV.Y, extV.Z, ModelUnits);
  }
  private Vector3d VectorASToAcad(ASVector3d vector)
  {
    return new Vector3d(vector.x * FactorFromNative, vector.y * FactorFromNative, vector.z * FactorFromNative);
  }

  private Box BoxToSpeckle(ASBoundBlock3d bound)
  {
    try
    {
      bound.GetMinMaxPoints(out var point1, out var point2);
      // convert min and max pts to speckle first
      var min = PointToSpeckle(point1);
      var max = PointToSpeckle(point2);

      // get dimension intervals
      var xSize = new Interval(min.x, max.x);
      var ySize = new Interval(min.y, max.y);
      var zSize = new Interval(min.z, max.z);

      // get the base plane of the bounding box from extents and current UCS
      var ucs = Doc.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
      var plane = new AcadGeo.Plane(PointASToAcad(point1), ucs.Xaxis, ucs.Yaxis);

      var box = new Box()
      {
        xSize = xSize,
        ySize = ySize,
        zSize = zSize,
        basePlane = PlaneToSpeckle(plane),
        volume = xSize.Length * ySize.Length * zSize.Length,
        units = ModelUnits
      };

      return box;
    }
    catch
    {
      return null;
    }
  }
  private Box BoxToSpeckle(ASExtents extents)
  {
    try
    {
      // convert min and max pts to speckle first
      var min = PointToSpeckle(extents.MinPoint);
      var max = PointToSpeckle(extents.MaxPoint);

      // get dimension intervals
      var xSize = new Interval(min.x, max.x);
      var ySize = new Interval(min.y, max.y);
      var zSize = new Interval(min.z, max.z);

      // get the base plane of the bounding box from extents and current UCS
      var ucs = Doc.Editor.CurrentUserCoordinateSystem.CoordinateSystem3d;
      var plane = new AcadGeo.Plane(PointASToAcad(extents.MinPoint), ucs.Xaxis, ucs.Yaxis);

      var box = new Box()
      {
        xSize = xSize,
        ySize = ySize,
        zSize = zSize,
        basePlane = PlaneToSpeckle(plane),
        volume = xSize.Length * ySize.Length * zSize.Length,
        units = ModelUnits
      };

      return box;
    }
    catch
    {
      return null;
    }
  }

  private Polycurve PolycurveToSpeckle(ASPolyline3d polyline)
  {
    var units = ModelUnits;
    Polycurve specklePolycurve = new(units) { closed = polyline.IsClosed };

    polyline.GetCurves(out ASCurve3d[] foundPolyCurves);
    for (int i = 0; i < foundPolyCurves.Length; i++)
    {
      ASCurve3d nextCurve = foundPolyCurves[i];
      if (nextCurve is ASLineSeg3d line)
      {
        specklePolycurve.segments.Add(LineToSpeckle(line));
      }

      if (nextCurve is ASCircArc3d arc)
      {
        specklePolycurve.segments.Add(ArcToSpeckle(arc));
      }
    }
    return specklePolycurve;
  }

  private Polycurve PolycurveToSpeckle(ASPoint3d[] pointsContour)
  {
    var units = ModelUnits;
    var specklePolycurve = new Polycurve(units);

    for (int i = 1; i < pointsContour.Length; i++)
    {
      specklePolycurve.segments.Add(LineToSpeckle(pointsContour[i - 1], pointsContour[i]));
    }

    specklePolycurve.segments.Add(LineToSpeckle(pointsContour.Last(), pointsContour.First()));

    return specklePolycurve;
  }

  private Line LineToSpeckle(ASPoint3d point1, ASPoint3d point2)
  {
    return new Line(PointToSpeckle(point1), PointToSpeckle(point2), ModelUnits);
  }

  private Line LineToSpeckle(ASLineSeg3d line)
  {
    var _line = new Line(PointToSpeckle(line.StartPoint), PointToSpeckle(line.EndPoint), ModelUnits);
    _line.length = line.GetLength();

    if (line.HasStartParam(out var start) && line.HasEndParam(out var end))
    {
      _line.domain = new Interval(start, end);
    }

    _line.bbox = BoxToSpeckle(line.GetOrthoBoundBlock());
    return _line;
  }

  private Arc ArcToSpeckle(ASCircArc3d arc)
  {
    Arc _arc;

    if (arc.IsPlanar(out var plane))
    {
      _arc = new Arc(PlaneToSpeckle(plane), PointToSpeckle(arc.StartPoint), PointToSpeckle(arc.EndPoint), arc.IncludedAngle, ModelUnits);
    }
    else
    {
      _arc = new Arc(PointToSpeckle(arc.StartPoint), PointToSpeckle(arc.EndPoint), arc.IncludedAngle, ModelUnits);
    }

    _arc.midPoint = PointToSpeckle(arc.MidPoint);

    if (arc.HasStartParam(out var start) && arc.HasEndParam(out var end))
    {
      _arc.domain = new Interval(start, end);
    }

    _arc.length = arc.GetLength();
    _arc.bbox = BoxToSpeckle(arc.GetOrthoBoundBlock());
    return _arc;
  }

  private Plane PlaneToSpeckle(ASPlane plane)
  {
    plane.GetCoordSystem(out var origin, out var vectorX, out var vectorY, out var vectorZ);

    return new Plane(PointToSpeckle(origin), VectorToSpeckle(plane.Normal), VectorToSpeckle(vectorX), VectorToSpeckle(vectorY), ModelUnits);
  }

  private object ConvertValueToSpeckle(object @object, eUnitType? unitType, out bool converted)
  {
    converted = true;
    if (@object is ASPoint3d)
    {
      return PointToSpeckle(@object as ASPoint3d);
    }
    else if (@object is ASVector3d)
    {
      return VectorToSpeckle(@object as ASVector3d);
    }
    else if (IsValueGenericList(@object))
    {
      IList list = @object as IList;
      if (list.Count == 0)
      {
        return null;
      }

      List<object> listReturn = new();
      foreach (var item in list)
      {
        listReturn.Add(ConvertValueToSpeckle(item, unitType, out _));
      }

      return listReturn;
    }
    else if (IsValueGenericDictionary(@object))
    {
      IDictionary dictionary = @object as IDictionary;
      if (dictionary.Count == 0)
      {
        return null;
      }

      Dictionary<object, object> dictionaryReturn = new();
      foreach (var key in dictionary.Keys)
      {
        dictionaryReturn.Add(key, ConvertValueToSpeckle(dictionary[key], unitType, out _));
      }

      return dictionaryReturn;
    }
    else
    {
      if(unitType.HasValue && @object is double)
      {
        @object = FromInternalUnits((double)@object, unitType.Value);
      }

      converted = false;
      return @object;
    }
  }

  private static bool IsValueGenericList(object value)
  {
    var type = value.GetType();
    return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>));
  }

  private static bool IsValueGenericDictionary(object value)
  {
    var type = value.GetType();
    return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>));
  }

  private double FromInternalUnits(double value, eUnitType unitType)
  {
    double valueScaled = value * GetUnitScaleFromNative(unitType);

    if (unitType == eUnitType.kWeight)
    {
      valueScaled = RoundBigDecimalNumbers(valueScaled, 5);
    }
    else if (unitType == eUnitType.kVolume)
    {
      if (valueScaled > 999)
      {
        valueScaled = RoundBigDecimalNumbers(valueScaled, 3);
      }
      else
      {
        valueScaled = RoundBigDecimalNumbers(valueScaled, 9);
      }
    }
    else if (unitType == eUnitType.kArea)
    {
      if (valueScaled > 999)
      {
        valueScaled = RoundBigDecimalNumbers(valueScaled, 2);
      }
      else
      {
        valueScaled = RoundBigDecimalNumbers(valueScaled, 6);
      }
    }

    return valueScaled;
  }

  private static double RoundBigDecimalNumbers(double value, int digits)
  {
    return Math.Round(value, digits, MidpointRounding.AwayFromZero);
  }

#region Units

  private UnitsSet _unitsSet;

  private UnitsSet UnitsSet
  {
    get
    {
      if (_unitsSet == null)
      {
        _unitsSet = DocumentManager.GetCurrentDocument().CurrentDatabase.Units;

        //Workaround to fix strange beahaviour when we are using the modeler of some beams(lost some faces)
        var unitOriginal = _unitsSet.UnitOfArea;
        _unitsSet.UnitOfArea = new Unit();
        _unitsSet.UnitOfArea = unitOriginal;
      }

      return _unitsSet;
    }
  }

  private double GetUnitScaleFromNative(eUnitType unitType)
  {
    return 1 / UnitsSet.GetUnit(unitType).Factor;
  }

  private double _factorFromNative;
  private double FactorFromNative
  {
    get
    {
      if (_factorFromNative.Equals(0.0))
      {
        _factorFromNative = 1 / DocumentManager.GetCurrentDocument().CurrentDatabase.Units.UnitOfDistance.Factor;
      }

      return _factorFromNative;
    }
  }

  private string unitWeight;
  private string UnitWeight
  {
    get
    {
      if (string.IsNullOrEmpty(unitWeight))
      {
        unitWeight = UnitsSet.GetUnit(eUnitType.kWeight).Symbol;
      }
      return unitWeight;
    }
  }

  private string unitVolume;
  private string UnitVolume
  {
    get
    {
      if (string.IsNullOrEmpty(unitVolume))
      {
        unitVolume = UnitsSet.GetUnit(eUnitType.kVolume).Symbol;
      }
      return unitVolume;
    }
  }

  private string unitArea;
  private string UnitArea
  {
    get
    {
      if (string.IsNullOrEmpty(unitArea))
      {
        unitArea = UnitsSet.GetUnit(eUnitType.kArea).Symbol;
      }
      return unitArea;
    }
  }

#endregion
}
#endif
