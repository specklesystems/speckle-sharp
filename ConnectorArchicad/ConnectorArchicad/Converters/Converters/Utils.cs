using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Archicad.Model;
using Objects;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace Archicad.Converters;

public static class Utils
{
  private const string MATERIAL_QUANTITIES_TAG = "materialQuantities";

  public static Point VertexToPoint(MeshModel.Vertex vertex)
  {
    return new Point
    {
      x = vertex.x,
      y = vertex.y,
      z = vertex.z
    };
  }

  public static Vector VertexToVector(MeshModel.Vertex vertex)
  {
    return new Vector
    {
      x = vertex.x,
      y = vertex.y,
      z = vertex.z
    };
  }

  public static System.Numerics.Vector3 VertexToVector3(MeshModel.Vertex vertex)
  {
    return new System.Numerics.Vector3
    {
      X = (float)vertex.x,
      Y = (float)vertex.y,
      Z = (float)vertex.z
    };
  }

  public static Point ScaleToNative(Point point, string? units = null)
  {
    units ??= point.units;
    var scale = Units.GetConversionFactor(units, Units.Meters);

    return new Point(point.x * scale, point.y * scale, point.z * scale);
  }

  public static double ScaleToNative(double value, string sourceUnits)
  {
    return value * Units.GetConversionFactor(sourceUnits, Units.Meters);
  }

  public static MeshModel.Vertex PointToNative(Point point, string? units = null)
  {
    units ??= point.units;
    var scale = Units.GetConversionFactor(units, Units.Meters);

    return new MeshModel.Vertex
    {
      x = point.x * scale,
      y = point.y * scale,
      z = point.z * scale
    };
  }

  public static Vector ScaleToNative(Vector vector, string? units = null)
  {
    units ??= vector.units;
    var scale = Units.GetConversionFactor(units, Units.Meters);

    return new Vector(vector.x * scale, vector.y * scale, vector.z * scale);
  }

  public static Polycurve PolycurveToSpeckle(ElementShape.Polyline archiPolyline)
  {
    var poly = new Polycurve
    {
      units = Units.Meters,
      closed = archiPolyline.polylineSegments.First().startPoint == archiPolyline.polylineSegments.Last().endPoint
    };
    foreach (var segment in archiPolyline.polylineSegments)
    {
      poly.segments.Add(
        segment.arcAngle == 0
          ? new Line(segment.startPoint, segment.endPoint)
          : new Arc(segment.startPoint, segment.endPoint, segment.arcAngle)
      );
    }

    return poly;
  }

  public static ElementShape.PolylineSegment LineToNative(Line line)
  {
    return new ElementShape.PolylineSegment(ScaleToNative(line.start), ScaleToNative(line.end));
  }

  public static ElementShape.Polyline PolycurveToNative(Polycurve polycurve)
  {
    var segments = polycurve.segments.Select(CurveSegmentToNative).ToList();
    return new ElementShape.Polyline(segments);
  }

  public static ElementShape.Polyline PolylineToNative(Polyline polyline)
  {
    var archiPoly = new ElementShape.Polyline();
    var points = polyline.GetPoints();
    points.ForEach(p => ScaleToNative(p));
    for (var i = 0; i < points.Count - 1; i++)
    {
      archiPoly.polylineSegments.Add(new ElementShape.PolylineSegment(points[i], points[i + 1]));
    }

    return archiPoly;
  }

  public static ElementShape.PolylineSegment ArcToNative(Arc arc)
  {
    return new ElementShape.PolylineSegment(
      ScaleToNative(arc.startPoint),
      ScaleToNative(arc.endPoint),
      arc.angleRadians
    );
  }

  public static ElementShape.Polyline? CurveToNative(ICurve curve)
  {
    return curve switch
    {
      Polyline polyline => PolylineToNative(polyline),
      Polycurve polycurve => PolycurveToNative(polycurve),
      _ => null
    };
  }

  public static ElementShape.PolylineSegment? CurveSegmentToNative(ICurve curve)
  {
    return curve switch
    {
      Line line => LineToNative(line),
      Arc arc => ArcToNative(arc),
      _ => throw new SpeckleException("Archicad Element Shapes can only be created with Lines or Arcs.")
    };
  }

  public static ElementShape PolycurvesToElementShape(ICurve outline, List<ICurve> voids = null)
  {
    var shape = new ElementShape(CurveToNative(outline));
    if (voids?.Count > 0)
    {
      shape.holePolylines = new List<ElementShape.Polyline>(voids.Select(CurveToNative));
    }

    return shape;
  }

  /// <summary>
  /// Convert incoming JSON (from Archicad) to an equivalent (specified) object type
  /// </summary>
  /// <param name="jObject">The incoming JSON (handled as a dynamic object)</param>
  /// <returns>An object of the specified type (T)</returns>
  public static T ConvertToSpeckleDTOs<T>(dynamic jObject)
    where T : Speckle.Core.Models.Base
  {
    Objects.BuiltElements.Archicad.ArchicadLevel level = null;
    if (jObject.level != null)
    {
      level = jObject.level.ToObject<Objects.BuiltElements.Archicad.ArchicadLevel>();
      jObject.Remove("level");
    }

    List<Objects.BuiltElements.Archicad.PropertyGroup> elementProperties = null;
    if (jObject.elementProperties != null)
    {
      elementProperties = jObject.elementProperties.ToObject<List<Objects.BuiltElements.Archicad.PropertyGroup>>();
      jObject.Remove("elementProperties");
    }

    List<Objects.BuiltElements.Archicad.ComponentProperties> componentProperties = null;
    if (jObject.componentProperties != null)
    {
      componentProperties = jObject.componentProperties.ToObject<
        List<Objects.BuiltElements.Archicad.ComponentProperties>
      >();
      jObject.Remove("componentProperties");
    }

    //Seek optional material quantities attached to the element (volume/area associated with a building material)
    List<MaterialQuantity> materialQuantities = null;
    if (jObject.materialQuantities != null)
    {
      materialQuantities = jObject.materialQuantities.ToObject<List<MaterialQuantity>>();
      jObject.Remove(MATERIAL_QUANTITIES_TAG);
    }

    T speckleObject = jObject.ToObject<T>();

    if (level != null)
    {
      PropertyInfo propLevel =
        speckleObject.GetType().GetProperty("archicadLevel") ?? speckleObject.GetType().GetProperty("level");
      propLevel.SetValue(speckleObject, level);
    }

    if (elementProperties != null)
    {
      PropertyInfo propElementProperties = speckleObject.GetType().GetProperty("elementProperties");
      propElementProperties.SetValue(speckleObject, PropertyGroup.ToBase(elementProperties));
    }

    if (componentProperties != null)
    {
      PropertyInfo propComponentProperties = speckleObject.GetType().GetProperty("componentProperties");
      propComponentProperties.SetValue(speckleObject, ComponentProperties.ToBase(componentProperties));
    }

    if (materialQuantities != null)
    {
      speckleObject[MATERIAL_QUANTITIES_TAG] = materialQuantities;
    }

    return speckleObject;
  }

  public static T ConvertToArchicadDTOs<T>(dynamic @object)
  {
    if (@object.elementProperties != null)
    {
      @object.elementProperties = null;
    }

    if (@object.componentProperties != null)
    {
      @object.componentProperties = null;
    }

    if (@object.GetType().GetProperty("elements") != null)
    {
      @object.elements = null;
    }

    return @object;
  }

  public static Objects.BuiltElements.Archicad.ArchicadLevel ConvertLevel(Objects.BuiltElements.Level level)
  {
    return (level == null)
      ? null
      : new Objects.BuiltElements.Archicad.ArchicadLevel
      {
        id = level.id,
        applicationId = level.applicationId,
        elevation = level.elevation * Units.GetConversionFactor(level.units, Units.Meters),
        name = level.name
      };
  }
}
