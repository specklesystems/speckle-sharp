using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Objects.Structural.Properties;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
#if REVIT2020 || REVIT2021 || REVIT2022
using ConverterRevitShared.Models;
using Point = Objects.Geometry.Point;
#endif

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public Objects.Geometry.Line GetBottomLine(List<Node> nodes)
  {
    Objects.Geometry.Line baseLine = new();
    double lowest_elv = nodes.Min(nodes => nodes.basePoint.z);
    List<Node> nodes1 = nodes.FindAll(node => node.basePoint.z.Equals(lowest_elv));
    if (nodes1.Count == 2)
    {
      var point1 = nodes1[0].basePoint;
      var point2 = nodes1[1].basePoint;
      baseLine = new Geometry.Line(point1, point2, point1.units);
      return baseLine;
    }
    return null;
  }

  public Objects.Geometry.Polycurve PolycurveFromTopology(List<Node> nodes)
  {
    Polycurve polycurve = new();
    foreach (int index in Enumerable.Range(0, nodes.Count))
    {
      if (index == nodes.Count - 1)
      {
        var point1 = nodes[index].basePoint;
        var point2 = nodes[0].basePoint;
        Geometry.Line segment = new(point1, point2, point1.units);
        polycurve.segments.Add(segment);
      }
      else
      {
        var point1 = nodes[index].basePoint;
        var point2 = nodes[index + 1].basePoint;
        Geometry.Line segment = new(point1, point2, point1.units);
        polycurve.segments.Add(segment);
      }
    }
    return polycurve;
  }

  public ApplicationObject AnalyticalSurfaceToNative(Element2D speckleElement)
  {
    var appObj = new ApplicationObject(speckleElement.id, speckleElement.speckle_type)
    {
      applicationId = speckleElement.applicationId
    };
    if (speckleElement.property is not Property2D prop2D)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "\"Property\" cannot be null");
      return appObj;
    }

#if REVIT2020 || REVIT2021 || REVIT2022
    appObj = CreatePhysicalMember(speckleElement);
    // TODO: set properties?
#else
    var elementType = GetElementType<ElementType>(speckleElement, appObj, out bool isExactMatch);
    if (elementType == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed);
      return appObj;
    }

    var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(
      Doc
    );

    // check for existing member
    var docObj = GetExistingElementByApplicationId(speckleElement.applicationId);

    // skip if element already exists in doc & receive mode is set to ignore
    if (IsIgnore(docObj, appObj))
    {
      return appObj;
    }

    AnalyticalPanel revitMember = null;
    DB.Element physicalMember = null;
    var isUpdate = false;

    if (docObj != null && docObj is AnalyticalPanel analyticalMember)
    {
      isUpdate = true;
      revitMember = analyticalMember;

      // TODO check if there are openings in the panel
      var polycurve = PolycurveFromTopology(speckleElement.topology);
      var curveArray = CurveToNative(polycurve, true);
      var curveLoop = CurveArrayToCurveLoop(curveArray);
      analyticalMember.SetOuterContour(curveLoop);

      if (isExactMatch && analyticalToPhysicalManager.HasAssociation(revitMember.Id))
      {
        //update type
        var physicalMemberId = analyticalToPhysicalManager.GetAssociatedElementId(revitMember.Id);
        physicalMember = Doc.GetElement(physicalMemberId);

        if (physicalMember.GetTypeId() != elementType.Id)
        {
          // collect info about current floor location and depth
          var currentType = Doc.GetElement(physicalMember.GetTypeId());
          var currentTypeDepth = GetParamValue<double>(
            currentType,
            BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM
          );
          var currentHeightOffset = GetParamValue<double>(
            physicalMember,
            BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM
          );

          // change type
          physicalMember.ChangeTypeId(elementType.Id);

          // make sure that the bottom of the floor remains in the same location
          var newTypeDepth = GetParamValue<double>(elementType, BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
          TrySetParam(
            physicalMember,
            BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM,
            currentHeightOffset + (newTypeDepth - currentTypeDepth)
          );
        }
      }
    }

    //create analytical panel (floor or wall)
    if (revitMember == null)
    {
      var polycurve = PolycurveFromTopology(speckleElement.topology);
      var curveArray = CurveToNative(polycurve, true);
      var curveLoop = CurveArrayToCurveLoop(curveArray);
      revitMember = AnalyticalPanel.Create(Doc, curveLoop);
    }

    // if there isn't an associated physical element to the analytical element, create it
    if (!analyticalToPhysicalManager.HasAssociation(revitMember.Id))
    {
      var physicalMemberAppObj = CreatePhysicalMember(speckleElement);
      physicalMember = (DB.Element)physicalMemberAppObj.Converted.FirstOrDefault();
      analyticalToPhysicalManager.AddAssociation(revitMember.Id, physicalMember.Id);

      appObj.Update(createdId: physicalMember.UniqueId, convertedItem: physicalMember);
    }

    var state = isUpdate ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
    appObj.Update(status: state, createdId: revitMember.UniqueId, convertedItem: revitMember);

#endif
    return appObj;
  }

  private ApplicationObject CreatePhysicalMember(Element2D speckleElement)
  {
    var appObj = new ApplicationObject(speckleElement.id, speckleElement.speckle_type);
    if (!(speckleElement.property is Property2D prop2D))
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "\"Property\" cannot be null");
      return appObj;
    }

    switch (prop2D.type)
    {
      case Structural.PropertyType2D.Wall:
        var baseline = GetBottomLine(speckleElement.topology);
        var lowestElvevation = speckleElement.topology.Min(node => node.basePoint.z);
        var topElevation = speckleElement.topology.Max(node => node.basePoint.z);
        var bottomNode = speckleElement.topology.Find(node => node.basePoint.z == lowestElvevation);
        var topNode = speckleElement.topology.Find(node => node.basePoint.z == topElevation);
        var bottemLevel = LevelFromPoint(PointToNative(bottomNode.basePoint));
        var topLevel = LevelFromPoint(PointToNative(topNode.basePoint));
        var revitWall = new RevitWall(
          speckleElement.property.name,
          speckleElement.property.name,
          baseline,
          bottemLevel,
          topLevel
        );
#if REVIT2020 || REVIT2021 || REVIT2022
        revitWall.applicationId = speckleElement.applicationId;
#endif
        return WallToNative(revitWall);

      default:
        var polycurve = PolycurveFromTopology(speckleElement.topology);
        var level = LevelFromPoint(PointToNative(speckleElement.topology[0].basePoint));
        var speckleFloor = new BuiltElements.Floor(polycurve);
        SetElementType(speckleFloor, speckleElement.property.name);
#if REVIT2020 || REVIT2021 || REVIT2022
        speckleFloor.applicationId = speckleElement.applicationId;
#endif
        return FloorToNative(speckleFloor);
    }
  }

#if REVIT2020 || REVIT2021 || REVIT2022
  private Element2D AnalyticalSurfaceToSpeckle(AnalyticalModelSurface revitSurface)
  {
    if (!revitSurface.IsEnabled())
    {
      return new Element2D();
    }

    var speckleElement2D = new Element2D();
    var structuralElement = revitSurface.Document.GetElement(revitSurface.GetElementId());
    var mark = GetParamValue<string>(structuralElement, BuiltInParameter.ALL_MODEL_MARK);
    speckleElement2D.name = mark;

    var openings = GetOpeningsAsPolylineFromSurface(revitSurface).ToList();
    var edgePoints = GetSurfaceOuterLoop(revitSurface).ToList();

    Element2DOutlineBuilder outlineBuilder = new(openings, edgePoints);

    speckleElement2D.openings = openings
      .Select(polyLine => new Polycurve(ModelUnits) { segments = new() { polyLine } })
      .ToList();

    speckleElement2D.topology = outlineBuilder.GetOutline().Select(p => new Node(p)).ToList();

    var prop = new Property2D();

    // Material
    DB.Material structMaterial = null;
    double thickness = 0;
    var memberType = PropertyType2D.Plate; // NOTE: a floor is typically classified as a plate since subjected to bending and shear stresses. Standard to have this as default.

    if (structuralElement is DB.Floor)
    {
      var floor = structuralElement as DB.Floor;
      structMaterial = floor.Document.GetElement(floor.FloorType.StructuralMaterialId) as DB.Material;
      thickness = GetParamValue<double>(structuralElement, BuiltInParameter.STRUCTURAL_FLOOR_CORE_THICKNESS);
    }
    else if (structuralElement is DB.Wall)
    {
      var wall = structuralElement as DB.Wall;
      structMaterial =
        wall.Document.GetElement(wall.WallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId())
        as DB.Material;
      thickness = ScaleToSpeckle(wall.WallType.Width);
      memberType = PropertyType2D.Shell; // NOTE: A wall is typically classified as shell since subjected to axial stresses
    }

    var speckleMaterial = GetStructuralMaterial(structMaterial);

    prop.material = speckleMaterial;

    prop.name = revitSurface.Document.GetElement(revitSurface.GetElementId()).Name;
    prop.type = memberType;
    prop.thickness = thickness;
    prop.units = ModelUnits;

    speckleElement2D.property = prop;

    GetAllRevitParamsAndIds(speckleElement2D, revitSurface);
    speckleElement2D.displayValue = GetElementDisplayValue(
      revitSurface.Document.GetElement(revitSurface.GetElementId())
    );

    return speckleElement2D;
  }

  private IEnumerable<Point> GetSurfaceOuterLoop(AnalyticalModelSurface surface)
  {
    IList<CurveLoop> loops = surface.GetLoops(AnalyticalLoopType.External);
    foreach (XYZ xyz in EnumerateCurveLoopWithMostPoints(loops))
    {
      yield return PointToSpeckle(xyz, surface.Document);
    }
  }

  private IEnumerable<Polyline> GetOpeningsAsPolylineFromSurface(AnalyticalModelSurface surface)
  {
    surface.GetOpenings(out ICollection<ElementId> openingIds);
    foreach (ElementId openingId in openingIds)
    {
      foreach (CurveLoop loop in surface.GetOpeningLoops(openingId))
      {
        IEnumerable<XYZ> points = EnumerateCurveLoopAsPoints(loop);
        List<double> coordinateList = points
          .Select(p => PointToSpeckle(p, surface.Document))
          .SelectMany(specklePoint => specklePoint.ToList())
          .ToList();

        // add back first point to close the polyline
        coordinateList.Add(coordinateList[0]);
        coordinateList.Add(coordinateList[1]);
        coordinateList.Add(coordinateList[2]);
        yield return new Polyline(coordinateList, ModelUnits);
      }
    }
  }

  /// <summary>
  /// Revit walls and floors can have multiple different areas that are part of the same wall.
  /// This isn't currently supported by our object model, and currently it is not possible to return multiple
  /// floors from floorToNative, so right now we're just converting the area that has the most line segments
  /// </summary>
  /// <param name="curveLoops"></param>
  /// <returns></returns>
  IEnumerable<XYZ> EnumerateCurveLoopWithMostPoints(IEnumerable<CurveLoop> curveLoops)
  {
    List<CurveLoop> curveLoopList = curveLoops.ToList();
    Dictionary<CurveLoop, int> loopCounts = new();
    foreach (var loop in curveLoopList)
    {
      loopCounts.Add(loop, loop.Count());
    }

    CurveLoop largestLoop = loopCounts.OrderByDescending(kvp => kvp.Value).First().Key;
    return EnumerateCurveLoopAsPoints(largestLoop);
  }

  IEnumerable<XYZ> EnumerateCurveLoopAsPoints(CurveLoop loop)
  {
    foreach (var curve in loop)
    {
      var points = curve.Tessellate();
      // here we are skipping the first point each time because
      // it is always the same as the last point of the previous curve
      foreach (var point in points.Skip(1))
      {
        yield return point;
      }
    }
  }
#else
  private Element2D AnalyticalSurfaceToSpeckle(AnalyticalPanel revitSurface)
  {
    var speckleElement2D = new Element2D();

    var structuralElement = revitSurface;

    var mark = GetParamValue<string>(structuralElement, BuiltInParameter.ALL_MODEL_MARK);
    speckleElement2D.name = mark;

    var edgeNodes = new List<Node> { };
    var loops = revitSurface.GetOuterContour();

    var displayLine = new Polycurve();
    foreach (var loop in loops)
    {
      var coor = new List<double>();

      var points = loop.Tessellate();

      foreach (var p in points.Skip(1))
      {
        var vertex = PointToSpeckle(p, revitSurface.Document);
        var edgeNode = new Node(vertex, null, null, null);
        edgeNodes.Add(edgeNode);
      }

      displayLine.segments.Add(CurveToSpeckle(loop, revitSurface.Document));
    }

    speckleElement2D.topology = edgeNodes;

    // Property and Material
    var prop = new Property2D();
    DB.Material structMaterial = null;
    double thickness = 0;
    var memberType = PropertyType2D.Plate; // NOTE: a floor is typically classified as a plate since subjected to bending and shear stresses. Standard to have this as default.

    if (structuralElement.StructuralRole is AnalyticalStructuralRole.StructuralRoleFloor)
    {
      structMaterial = structuralElement.Document.GetElement(structuralElement.MaterialId) as DB.Material;
      thickness = structuralElement.Thickness;
    }
    else if (structuralElement.StructuralRole is AnalyticalStructuralRole.StructuralRoleWall)
    {
      structMaterial = structuralElement.Document.GetElement(structuralElement.MaterialId) as DB.Material;
      thickness = structuralElement.Thickness;
      memberType = PropertyType2D.Shell; // NOTE: A wall is typically classified as shell since subjected to axial stresses
    }

    var speckleMaterial = GetStructuralMaterial(structMaterial);
    prop.material = speckleMaterial;

    prop.name = structuralElement.Name; // NOTE: This is typically "" for analytical surfaces
    prop.type = memberType;
    prop.thickness = ScaleToSpeckle(thickness);
    prop.units = ModelUnits;

    var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(
      Doc
    );
    if (analyticalToPhysicalManager.HasAssociation(revitSurface.Id))
    {
      var physicalElementId = analyticalToPhysicalManager.GetAssociatedElementId(revitSurface.Id);
      var physicalElement = Doc.GetElement(physicalElementId);
      speckleElement2D.displayValue = GetElementDisplayValue(physicalElement);
      prop.name = physicalElement.Name; // Rather use the name of the associated physical type (better than an empty string)
    }

    speckleElement2D.property = prop;
    speckleElement2D.openings = GetOpenings(revitSurface);

    GetAllRevitParamsAndIds(speckleElement2D, revitSurface);

    return speckleElement2D;
  }

  private List<Polycurve> GetOpenings(AnalyticalPanel revitSurface)
  {
    var openings = new List<Polycurve>();
    foreach (var openingId in revitSurface.GetAnalyticalOpeningsIds())
    {
      if (revitSurface.Document.GetElement(openingId) is not AnalyticalOpening opening)
      {
        continue;
      }

      var curveLoop = opening.GetOuterContour();
      openings.Add(CurveLoopToSpeckle(curveLoop, revitSurface.Document));
    }
    return openings;
  }
#endif
}
