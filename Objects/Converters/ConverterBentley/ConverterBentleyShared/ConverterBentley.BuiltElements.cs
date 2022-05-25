#if (OPENBUILDINGS)
using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using Bentley.ECObjects;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.ECObjects.XML;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;

using Bentley.Building.Api;

using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using BIM = Bentley.Interop.MicroStationDGN;
using BMIU = Bentley.MstnPlatformNET.InteropServices.Utilities;
using Box = Objects.Geometry.Box;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using FamilyInstance = Objects.BuiltElements.Revit.FamilyInstance;
using Interval = Objects.Primitive.Interval;
using Level = Objects.BuiltElements.Level;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Parameter = Objects.BuiltElements.Revit.Parameter;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using RevitBeam = Objects.BuiltElements.Revit.RevitBeam;
using RevitColumn = Objects.BuiltElements.Revit.RevitColumn;
using RevitFloor = Objects.BuiltElements.Revit.RevitFloor;
using RevitWall = Objects.BuiltElements.Revit.RevitWall;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Bentley
{
  public partial class ConverterBentley
  {
    private static int Decimals = 3;

    private Dictionary<int, Level> levels = new Dictionary<int, Level>();

    public RevitBeam BeamToSpeckle(Dictionary<string, object> properties, string units = null)
    {
      var u = units ?? ModelUnits;

      string part = (string)GetProperty(properties, "PART");
      string family = (string)GetProperty(properties, "FAMILY");
      // for some reason the ElementID is a long
      int elementId = (int)(double)GetProperty(properties, "ElementID");
      DPoint3d start = (DPoint3d)GetProperty(properties, "PTS_0");
      DPoint3d end = (DPoint3d)GetProperty(properties, "PTS_1");

      List<Parameter> parameters = new List<Parameter>();
      // justification
      parameters.AddRange(CreateParameter(properties, "PLACEMENT_POINT", u));
      // rotation
      parameters.AddRange(CreateParameter(properties, "ROTATION", u));

      Line baseLine = LineToSpeckle(ToInternalCoordinates(start), ToInternalCoordinates(end));
      Level level = new Level();
      level.units = u;

      RevitBeam beam = new RevitBeam(family, part, baseLine, level, parameters);
      beam.elementId = elementId.ToString();
      //beam.displayMesh
      beam.units = u;

      return beam;
    }

    public RevitColumn ColumnToSpeckle(Dictionary<string, object> properties, string units = null)
    {
      var u = units ?? ModelUnits;

      string part = (string)GetProperty(properties, "PART");
      string family = (string)GetProperty(properties, "FAMILY");
      // for some reason the ElementID is a long
      int elementId = (int)(double)GetProperty(properties, "ElementID");
      DPoint3d start = (DPoint3d)GetProperty(properties, "PTS_0");
      DPoint3d end = (DPoint3d)GetProperty(properties, "PTS_1");
      double rotation = (double)GetProperty(properties, "ROTATION");
      double rotationZ = (double)GetProperty(properties, "RotationZ");

      Line baseLine = LineToSpeckle(ToInternalCoordinates(start), ToInternalCoordinates(end));

      double bottomElevation, topElevation;
      if (start.Z > end.Z)
      {
        bottomElevation = end.Z;
        topElevation = start.Z;
      }
      else
      {
        bottomElevation = start.Z;
        topElevation = end.Z;
      }

      Level level = CreateLevel(bottomElevation, u);
      Level topLevel = CreateLevel(topElevation, u);
      double baseOffset = 0;
      double topOffset = 0;
      bool structural = true;

      List<Parameter> parameters = new List<Parameter>();
      // justification
      parameters.AddRange(CreateParameter(properties, "PLACEMENT_POINT", u));

      RevitColumn column = new RevitColumn(family, part, baseLine, level, topLevel, baseOffset, topOffset, structural, rotationZ, parameters);
      column.elementId = elementId.ToString();
      //column.displayMesh
      column.units = u;

      return column;
    }

    public Level CreateLevel(double elevation, string units = null)
    {
      var u = units ?? ModelUnits;
      elevation = Math.Round(elevation, Decimals);

      int levelKey = (int)(elevation * Math.Pow(10, Decimals));
      levels.TryGetValue(levelKey, out Level level);

      if (level == null)
      {
        level = new Level("Level " + elevation + u, elevation);
        level.units = u;
        levels.Add(levelKey, level);
      }
      return level;
    }

    public List<Parameter> CreateParameter(Dictionary<string, object> properties, string propertyName, string units = null)
    {
      var u = units ?? ModelUnits;

      switch (propertyName)
      {
        // justification
        case ("PLACEMENT_POINT"):
          int placementPoint = (int)GetProperty(properties, "PLACEMENT_POINT");

          Parameter zJustification = new Parameter("z Justification", 0, u);
          zJustification.applicationInternalName = "Z_JUSTIFICATION";
          Parameter yJustification = new Parameter("y Justification", 0, u);
          yJustification.applicationInternalName = "Y_JUSTIFICATION";

          // Revit ZJustification
          // Top = 0
          // Center = 1
          // Origin = 2
          // Bottom = 3

          // Revit YJustification
          // Left = 0
          // Center = 1
          // Origin = 2
          // Right = 3

          switch (placementPoint)
          {
            case (1):
              // bottom left
              zJustification.value = 3;
              yJustification.value = 0;
              break;

            case (2):
              // bottom center
              zJustification.value = 3;
              yJustification.value = 1;
              break;

            case (3):
              // bottom right
              zJustification.value = 3;
              yJustification.value = 3;
              break;

            case (4):
              // center left
              zJustification.value = 1;
              yJustification.value = 0;
              break;

            case (10):
              // origin origin
              zJustification.value = 2;
              yJustification.value = 2;
              break;

            case (5):
              // center center
              zJustification.value = 1;
              yJustification.value = 1;
              break;

            case (6):
              // center right
              zJustification.value = 1;
              yJustification.value = 3;
              break;

            case (7):
              // top left
              zJustification.value = 0;
              yJustification.value = 0;
              break;

            case (8):
              // top center
              zJustification.value = 0;
              yJustification.value = 1;
              break;

            case (9):
              // top right
              zJustification.value = 0;
              yJustification.value = 3;
              break;

            default:
              zJustification.value = 0;
              yJustification.value = 0;
              break;
          }

          return new List<Parameter>() { zJustification, yJustification };

        case ("ROTATION"):
          double rotation = (double)GetProperty(properties, "ROTATION");
          Parameter structuralBendDirAngle = new Parameter("Cross-Section Rotation", -Math.PI * rotation / 180.0);
          return new List<Parameter>() { structuralBendDirAngle };

        default:
          throw new SpeckleException("Parameter for property not implemented.");
      }
    }

    public Polycurve CreateClosedPolyCurve(List<ICurve> lines, string units = null)
    {
      var u = units ?? ModelUnits;
      Polycurve polyCurve = new Polycurve(u);

      // sort lines
      List<ICurve> segments = Sort(lines);
      polyCurve.segments = segments;

      //polyCurve.domain
      polyCurve.closed = true;
      //polyCurve.bbox
      //polyCurve.area
      //polyCurve.length

      return polyCurve;
    }

    private List<ICurve> Sort(List<ICurve> lines)
    {
      double eps = 0.001;

      List<ICurve> sortedLines = new List<ICurve>();
      if (lines.Count > 0)
      {
        Line firstSegment = lines[0] as Line;
        Point currentEnd = firstSegment.end;
        sortedLines.Add(firstSegment);
        lines.Remove(firstSegment);
        int i = 0;
        while (lines.Count > 0)
        {
          if (i == lines.Count)
          {
            break;
          }
          ICurve nextSegment = lines[i];
          i++;
          Point nextStart = ((Line)nextSegment).start;
          Point nextEnd = ((Line)nextSegment).end;

          double dx = Math.Abs(nextStart.x - currentEnd.x);
          double dy = Math.Abs(nextStart.y - currentEnd.y);
          double dz = Math.Abs(nextStart.z - currentEnd.z);

          if (dx < eps && dy < eps && dz < eps)
          {
            sortedLines.Add(nextSegment);
            lines.Remove(nextSegment);

            currentEnd = ((Line)nextSegment).end;
            i = 0;
          }
        }
      }
      return sortedLines;
    }

    public Line CreateWallBaseLine(List<ICurve> shortEdges, string units = null)
    {
      var u = units ?? ModelUnits;

      Line edge1 = (Line)shortEdges[0];
      Line edge2 = (Line)shortEdges[1];

      double dx1 = edge1.end.x - edge1.start.x;
      double dy1 = edge1.end.y - edge1.start.y;
      double dz1 = edge1.end.z - edge1.start.z;

      double dx2 = edge2.end.x - edge2.start.x;
      double dy2 = edge2.end.y - edge2.start.y;
      double dz2 = edge2.end.z - edge2.start.z;

      // z-coordinates need to be rounded to avoid problems in Revit regarding floating point errors or small deviations
      double x1 = edge1.start.x + dx1 / 2;
      double y1 = edge1.start.y + dy1 / 2;
      double z1 = Math.Round(edge1.start.z + dz1 / 2, Decimals);

      double x2 = edge2.start.x + dx2 / 2;
      double y2 = edge2.start.y + dy2 / 2;
      double z2 = Math.Round(edge2.start.z + dz2 / 2, Decimals);

      Point start = new Point(x1, y1, z1, u);
      Point end = new Point(x2, y2, z2, u);

      Line baseLine = new Line(start, end, u);
      return baseLine;
    }

    private FamilyInstance CappingBeamToSpeckle(Dictionary<string, object> properties, string units = null)
    {
      var u = units ?? ModelUnits;
      string part = (string)GetProperty(properties, "PART");
      string family = (string)GetProperty(properties, "FAMILY");
      // for some reason the ElementID is a long
      int elementId = (int)(double)GetProperty(properties, "ElementID");
      DPoint3d start = (DPoint3d)GetProperty(properties, "PTS_0");
      DPoint3d end = (DPoint3d)GetProperty(properties, "PTS_1");
      double rotation = (double)GetProperty(properties, "ROTATION");
      double rotationZ = (double)GetProperty(properties, "RotationZ");

      Point basePoint = Point3dToSpeckle(start);
      string type = part;

      Level level = CreateLevel(basePoint.z, u);

      bool facingFlipped = false;
      bool handFlipped = false;
      FamilyInstance familyInstance = new FamilyInstance(basePoint, family, type, level, rotationZ, facingFlipped, handFlipped, new List<Parameter>());
      familyInstance.category = "Structural Foundations";
      familyInstance.elementId = elementId.ToString();
      return familyInstance;
    }

    public FamilyInstance PileToSpeckle(Dictionary<string, object> properties, string units = null)
    {
      var u = units ?? ModelUnits;
      string part = (string)GetProperty(properties, "PART");
      string family = (string)GetProperty(properties, "FAMILY");
      // for some reason the ElementID is a long
      int elementId = (int)(double)GetProperty(properties, "ElementID");
      DPoint3d start = (DPoint3d)GetProperty(properties, "PTS_0");
      DPoint3d end = (DPoint3d)GetProperty(properties, "PTS_1");
      double rotation = (double)GetProperty(properties, "ROTATION");
      double rotationZ = (double)GetProperty(properties, "RotationZ");

      Point basePoint;
      if (start.Z > end.Z)
      {
        basePoint = Point3dToSpeckle(ToInternalCoordinates(start));
      }
      else
      {
        basePoint = Point3dToSpeckle(ToInternalCoordinates(end));
      }
      string type = part;

      Level level = CreateLevel(basePoint.z, u);
      basePoint.z = 0.0;

      bool facingFlipped = false;
      bool handFlipped = false;
      FamilyInstance familyInstance = new FamilyInstance(basePoint, family, type, level, rotationZ, facingFlipped, handFlipped, new List<Parameter>());
      familyInstance.category = "Structural Foundations";
      familyInstance.elementId = elementId.ToString();
      return familyInstance;
    }

    public Element RevitWallToNative(RevitWall wall)
    {
      if (wall.baseLine is Line baseLine)
      {
        baseLine.start.z += wall.baseOffset;
        baseLine.end.z += wall.baseOffset;

        DPoint3d start = Point3dToNative(baseLine.start);
        DPoint3d end = Point3dToNative(baseLine.end);

        double height = wall.height + wall.topOffset;
        //double thickness = height / 10.0;

        TFCatalogList datagroup = new TFCatalogList();
        datagroup.Init("");
        ITFCatalog catalog = datagroup as ITFCatalog;

        catalog.GetAllCatalogTypesList(0, out ITFCatalogTypeList typeList);

        string family = wall.family;
        string type = wall.type;

        catalog.GetCatalogItemByNames(family, type, 0, out ITFCatalogItemList itemList);

        // if no catalog item is found, use a random one
        if (itemList == null)
          catalog.GetCatalogItemsByTypeName("Wall", 0, out itemList);

        TFLoadableWallList form = new TFLoadableWallList();
        form.InitFromCatalogItem(itemList, 0);
        form.SetWallType(TFdLoadableWallType.TFdLoadableWallType_Line, 0);
        start.ScaleInPlace(1.0 / UoR);
        end.ScaleInPlace(1.0 / UoR);
        form.SetEndPoints(ref start, ref end, 0);
        form.SetHeight(height, 0);
        //form.SetThickness(thickness, 0);

        // todo: horizontal offset
        // revit parameter: WALL_KEY_REF_PARAM  "Location Line"
        // 0. wall centerline
        // 1. core centerline
        // 2. finish face: exterior
        // 3. finish face: interior
        // 4. core face: exterior
        // 5. core face: interior
        // 
        // todo: determine interior/exterior face?
        // form.SetOffsetType(TFdFormRecipeOffsetType.tfdFormRecipeOffsetTypeCenter, 0);

        form.GetElementWritten(out Element element, Session.Instance.GetActiveDgnModelRef(), 0);
        return element;
      }
      else
      {
        throw new SpeckleException("Only simple lines as base lines supported.");
      }
    }

    public RevitFloor SlabToSpeckle(Dictionary<string, object> properties, List<ICurve> segments, string units = null)
    {
      RevitFloor floor = new RevitFloor();
      var u = units ?? ModelUnits;

      string part = (string)GetProperty(properties, "PART");
      string family = "Floor";
      // for some reason the ElementID is a long
      int elementId = (int)(double)GetProperty(properties, "ElementID");

      Dictionary<int, List<ICurve>> elevationMap = new Dictionary<int, List<ICurve>>();
      int maxElevation = int.MinValue;

      // this should take the used units into account
      double epsilon = 0.001;

      foreach (ICurve segment in segments)
      {
        Line line = (Line)segment;
        Point start = line.start;
        Point end = line.end;

        double dx = Math.Abs(start.x - end.x);
        double dy = Math.Abs(start.y - end.y);
        double dz = Math.Abs(start.z - end.z);

        // drop vertical segments
        if (dx < epsilon && dy < epsilon)
        {
          continue;
        }

        if (dz > epsilon)
        {
          throw new SpeckleException("Inclined slabs not supported!");
        }

        int elevation = (int)Math.Round(start.z / epsilon);
        if (elevation > maxElevation)
        {
          maxElevation = elevation;
        }
        if (elevationMap.ContainsKey(elevation))
        {
          elevationMap[elevation].Add(line);
        }
        else
        {
          elevationMap.Add(elevation, new List<ICurve>() { line });
        }
      }

      if (elevationMap.Count != 2)
      {
        throw new SpeckleException("Slab geometry has more than two different elevations!");
      }

      Level level = CreateLevel(maxElevation, u);

      List<ICurve> lines = elevationMap[maxElevation];

      // todo: create bbox and sort by size
      // for now assuming that outline comes before the openings
      Polycurve outline = CreateClosedPolyCurve(lines, u);

      // all lines that are not part of the outline must be part of a void
      List<ICurve> voids = new List<ICurve>();
      while (lines.Count > 0)
      {
        Polycurve opening = CreateClosedPolyCurve(lines);
        voids.Add(opening);
      }

      floor.outline = outline;
      floor.voids = voids;
      //floor.elements
      floor.units = u;
      floor.type = part;
      floor.family = family;
      floor.elementId = elementId.ToString();
      floor.level = level;
      floor.structural = true;
      floor.slope = 0;
      //floor.slopeDirection

      return floor;
    }

    public RevitWall WallToSpeckle(Dictionary<string, object> properties, List<ICurve> segments, string units = null)
    {
      RevitWall wall = new RevitWall();

      var u = units ?? ModelUnits;
      string part = (string)GetProperty(properties, "PART");
      string family = "Basic Wall";
      // for some reason the ElementID is a long
      int elementId = (int)(double)GetProperty(properties, "ElementID");

      Dictionary<int, List<ICurve>> elevationMap = new Dictionary<int, List<ICurve>>();

      // this should take the used units into account
      double epsilon = 0.001;

      // only simple walls supported so far
      if (segments.Count != 12)
      {
        throw new SpeckleException("Wall geoemtry not supported!");
      }

      // sort segments by segment.length
      List<ICurve> sortedSegments = segments.OrderBy(segment => segment.length).ToList();

      // drop long edges
      sortedSegments.RemoveRange(4, 8);

      foreach (ICurve segment in sortedSegments)
      {
        Line line = (Line)segment;
        Point start = line.start;
        Point end = line.end;

        double dx = Math.Abs(start.x - end.x);
        double dy = Math.Abs(start.y - end.y);
        double dz = Math.Abs(start.z - end.z);

        // drop vertical edges
        if (dx < epsilon && dy < epsilon)
        {
          // there should be none
          continue;
        }

        if (dz > epsilon)
        {
          throw new SpeckleException("Wall geoemtry not supported!");
        }
        else
        {
          int currentElevation = (int)Math.Round(start.z / epsilon);
          if (elevationMap.ContainsKey(currentElevation))
          {
            elevationMap[currentElevation].Add(line);
          }
          else
          {
            List<ICurve> lines = new List<ICurve>() { line };
            elevationMap.Add(currentElevation, lines);
          }
        }
      }

      if (elevationMap.Count != 2)
      {
        throw new SpeckleException("Inclined walls not supported!");
      }

      // sort by elevations
      List<int> sortedElevations = elevationMap.Keys.OrderBy(lines => lines).ToList();

      Line baseLine = CreateWallBaseLine(elevationMap[sortedElevations[0]], u);

      double elevation = sortedElevations[0] * epsilon;
      double topElevation = sortedElevations[1] * epsilon;
      double height = topElevation - elevation;

      Level level = CreateLevel(elevation, u);
      Level topLevel = CreateLevel(topElevation, u);

      wall.height = height;
      //wall.elements = 
      wall.baseLine = baseLine;
      wall.units = u;
      wall.family = family;
      wall.type = part;
      wall.baseOffset = 0;
      wall.topOffset = 0;
      wall.flipped = false;
      wall.structural = true;
      wall.level = level;
      wall.topLevel = topLevel;
      wall.elementId = elementId.ToString();

      return wall;
    }

    private DPoint3d ToInternalCoordinates(DPoint3d point)
    {
      point.ScaleInPlace(UoR);
      return point;
    }

    enum Category
    {
      Beams,
      CappingBeams,
      Columns,
      FoundationSlabs,
      None,
      Piles,
      Slabs,
      Walls
    }
  }
}
#endif
