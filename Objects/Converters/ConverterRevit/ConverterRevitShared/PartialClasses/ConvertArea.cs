using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Utils;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  //public List<ApplicationObject> AreaToNative(BuiltElements.Area speckleArea)
  //{
  //  var revitRoom = GetExistingElementByApplicationId(speckleArea.applicationId) as DB.Area;
  //  var level = LevelToNative(speckleArea.level);


  //  //TODO: support updating rooms
  //  if (revitRoom != null)
  //  {
  //    Doc.Delete(revitRoom.Id);
  //  }

  //  revitRoom = Doc.Create.NewArea(level, new UV(speckleArea.center.x, speckleArea.center.y));

  //  revitRoom.Name = speckleArea.name;
  //  revitRoom.Number = speckleArea.number;

  //  SetInstanceParameters(revitRoom, speckleArea);

  //  var placeholders = new List<ApplicationObject>()
  //  {
  //    new ApplicationObject
  //    {
  //    applicationId = speckleArea.applicationId,
  //    ApplicationGeneratedId = revitRoom.UniqueId,
  //    NativeObject = revitRoom
  //    }
  //  };

  //  return placeholders;

  //}

  public BuiltElements.Area AreaToSpeckle(DB.Area revitArea)
  {
    var profiles = GetProfiles(revitArea);

    var speckleArea = new BuiltElements.Area
    {
      name = revitArea.get_Parameter(BuiltInParameter.ROOM_NAME).AsString(),
      number = revitArea.Number,
      center = (Point)LocationToSpeckle(revitArea),
      level = ConvertAndCacheLevel(revitArea, BuiltInParameter.ROOM_LEVEL_ID)
    };
    if (profiles.Count != 0)
    {
      speckleArea.outline = profiles[0];
    }

    speckleArea.area = GetParamValue<double>(revitArea, BuiltInParameter.ROOM_AREA);
    if (profiles.Count > 1)
    {
      speckleArea.voids = profiles.Skip(1).ToList();
    }

    GetAllRevitParamsAndIds(speckleArea, revitArea);

    //no mesh seems to be retrievable, not even using the SpatialElementGeometryCalculator
    //speckleArea.displayValue = GetElementDisplayValue(revitArea);

    speckleArea.displayValue ??= new List<Base>();

    if (profiles.Count != 0 && profiles[0] is Polycurve polyCurve)
    {
      speckleArea.displayValue.Add(polyCurve);
    }

    // If life were simple this triangulation world be sufficient - we know areas are 2d planar - but could have curves.
    // speckleArea.displayValue.Add(PolycurveToMesh(speckleArea.outline as Polycurve));

    return speckleArea;
  }

  private static Mesh PolycurveToMesh(Polycurve polycurve)
  {
    var mesh = new Mesh { units = polycurve.units };

    // Convert all segments to Lines (assuming they are all Lines)
    var segments = polycurve.segments.OfType<Line>().ToList();

    var points = new List<Point>();
    foreach (var segment in segments.Where(segment => !PointExists(points, segment.start)))
    {
      points.Add(segment.start);
    }
    if (!polycurve.closed && !PointExists(points, segments.Last().end))
    {
      points.Add(segments.Last().end);
    }

    mesh.vertices = points.SelectMany(p => new List<double> { p.x, p.y, p.z }).ToList();

    mesh.faces = new List<int> { points.Count }; // First element is the number of vertices in the face
    mesh.faces.AddRange(Enumerable.Range(0, points.Count));

    mesh.TriangulateMesh();

    return mesh;
  }

  private static bool PointExists(List<Point> points, Point newPoint)
  {
    const double TOLERANCE = 1e-6; // Adjust this tolerance as needed
    return points.Any(
      p =>
        Math.Abs(p.x - newPoint.x) < TOLERANCE
        && Math.Abs(p.y - newPoint.y) < TOLERANCE
        && Math.Abs(p.z - newPoint.z) < TOLERANCE
    );
  }
}
