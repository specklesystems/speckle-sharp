using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Objects.Structural.Materials;
using Objects.Structural.Properties;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;


namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Objects.Geometry.Line GetBottomLine(List<Node> nodes)
    {
      Objects.Geometry.Line baseLine = new Objects.Geometry.Line();
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
      Polycurve polycurve = new Polycurve();
      foreach (int index in Enumerable.Range(0, nodes.Count))
      {
        if (index == nodes.Count - 1)
        {
          var point1 = nodes[index].basePoint;
          var point2 = nodes[0].basePoint;
          Geometry.Line segment = new Geometry.Line(point1, point2, point1.units);
          polycurve.segments.Add(segment);
        }
        else
        {
          var point1 = nodes[index].basePoint;
          var point2 = nodes[index + 1].basePoint;
          Geometry.Line segment = new Geometry.Line(point1, point2, point1.units);
          polycurve.segments.Add(segment);
        }
      }
      return polycurve;
    }

    public ApplicationObject AnalyticalSurfaceToNative(Element2D speckleElement)
    {
      var appObj = new ApplicationObject(speckleElement.id, speckleElement.speckle_type) { applicationId = speckleElement.applicationId };
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

      var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);

      // check for existing member
      var docObj = GetExistingElementByApplicationId(speckleElement.applicationId);

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

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
            var currentTypeDepth = GetParamValue<double>(currentType, BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
            var currentHeightOffset = GetParamValue<double>(physicalMember, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);

            // change type
            physicalMember.ChangeTypeId(elementType.Id);

            // make sure that the bottom of the floor remains in the same location
            var newTypeDepth = GetParamValue<double>(elementType, BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM);
            TrySetParam(physicalMember, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM, currentHeightOffset + (newTypeDepth - currentTypeDepth));
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
          var revitWall = new RevitWall(speckleElement.property.name, speckleElement.property.name, baseline, bottemLevel, topLevel);
#if REVIT2020 || REVIT2021 || REVIT2022
          revitWall.applicationId = speckleElement.applicationId;
#endif
          return WallToNative(revitWall);

        default:
          var polycurve = PolycurveFromTopology(speckleElement.topology);
          var level = LevelFromPoint(PointToNative(speckleElement.topology[0].basePoint));
          var revitFloor = new RevitFloor(speckleElement.property.name, speckleElement.property.name, polycurve, level, true);
#if REVIT2020 || REVIT2021 || REVIT2022
          revitFloor.applicationId = speckleElement.applicationId;
#endif
          return FloorToNative(revitFloor);
      }
    }

#if REVIT2020 || REVIT2021 || REVIT2022
    private Element2D AnalyticalSurfaceToSpeckle(AnalyticalModelSurface revitSurface)
    {
      if (!revitSurface.IsEnabled())
        return new Element2D();

      var speckleElement2D = new Element2D();
      var structuralElement = revitSurface.Document.GetElement(revitSurface.GetElementId());
      var mark = GetParamValue<string>(structuralElement, BuiltInParameter.ALL_MODEL_MARK);
      speckleElement2D.name = mark;

      var edgeNodes = new List<Node> { };
      var loops = revitSurface.GetLoops(AnalyticalLoopType.External);

      var displayLine = new Polycurve();
      foreach (var loop in loops)
      {
        var coor = new List<double>();
        foreach (var curve in loop)
        {
          var points = curve.Tessellate();

          foreach (var p in points.Skip(1))
          {
            var vertex = PointToSpeckle(p, revitSurface.Document);
            var edgeNode = new Node(vertex, null, null, null);
            edgeNodes.Add(edgeNode);
          }

          displayLine.segments.Add(CurveToSpeckle(curve, revitSurface.Document));
        }
      }

      speckleElement2D.topology = edgeNodes;
      speckleElement2D.displayValue = GetElementDisplayValue(revitSurface, new Options() { DetailLevel = ViewDetailLevel.Fine });

      var voidNodes = new List<List<Node>> { };
      var voidLoops = revitSurface.GetLoops(AnalyticalLoopType.Void);
      foreach (var loop in voidLoops)
      {
        var loopNodes = new List<Node>();
        foreach (var curve in loop)
        {
          var points = curve.Tessellate();

          foreach (var p in points.Skip(1))
          {
            var vertex = PointToSpeckle(p, revitSurface.Document);
            var voidNode = new Node(vertex, null, null, null);
            loopNodes.Add(voidNode);
          }
        }
        voidNodes.Add(loopNodes);
      }
      //speckleElement2D.voids = voidNodes;

      //var mesh = new Geometry.Mesh();
      //var solidGeom = GetElementSolids(structuralElement);
      //(mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(solidGeom);
      //speckleElement2D.baseMesh = mesh;	  

      var prop = new Property2D();

      // Material
      DB.Material structMaterial = null;
      double thickness = 0;
      var memberType = MemberType2D.Generic2D;

      if (structuralElement is DB.Floor)
      {
        var floor = structuralElement as DB.Floor;
        structMaterial = floor.Document.GetElement(floor.FloorType.StructuralMaterialId) as DB.Material;
        thickness = GetParamValue<double>(structuralElement, BuiltInParameter.STRUCTURAL_FLOOR_CORE_THICKNESS);
        memberType = MemberType2D.Slab;
      }
      else if (structuralElement is DB.Wall)
      {
        var wall = structuralElement as DB.Wall;
        structMaterial = wall.Document.GetElement(wall.WallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM).AsElementId()) as DB.Material;
        thickness = ScaleToSpeckle(wall.WallType.Width);
        memberType = MemberType2D.Wall;
      }

      var speckleMaterial = GetStructuralMaterial(structMaterial);

      prop.material = speckleMaterial;

      prop.name = revitSurface.Document.GetElement(revitSurface.GetElementId()).Name;
      //prop.type = memberType;
      //prop.analysisType = Structural.AnalysisType2D.Shell;
      prop.thickness = thickness;

      speckleElement2D.property = prop;

      GetAllRevitParamsAndIds(speckleElement2D, revitSurface);

      //speckleElement2D.displayMesh = GetElementDisplayMesh(Doc.GetElement(revitSurface.GetElementId()),
      // new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      return speckleElement2D;
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
      var analyticalToPhysicalManager = AnalyticalToPhysicalAssociationManager.GetAnalyticalToPhysicalAssociationManager(Doc);
      if (analyticalToPhysicalManager.HasAssociation(revitSurface.Id))
      {
        var physicalElementId = analyticalToPhysicalManager.GetAssociatedElementId(revitSurface.Id);
        var physicalElement = Doc.GetElement(physicalElementId);
        speckleElement2D.displayValue = GetElementDisplayValue(physicalElement, new Options() { DetailLevel = ViewDetailLevel.Fine });
      }

      speckleElement2D.openings = GetOpenings(revitSurface);

      var prop = new Property2D();

      // Material
      DB.Material structMaterial = null;
      double thickness = 0;
      var memberType = MemberType2D.Generic2D;

      if (structuralElement.StructuralRole is AnalyticalStructuralRole.StructuralRoleFloor)
      {
        structMaterial = structuralElement.Document.GetElement(structuralElement.MaterialId) as DB.Material;
        thickness = structuralElement.Thickness;
        memberType = MemberType2D.Slab;
      }
      else if (structuralElement.StructuralRole is AnalyticalStructuralRole.StructuralRoleWall)
      {

        structMaterial = structuralElement.Document.GetElement(structuralElement.MaterialId) as DB.Material;
        thickness = structuralElement.Thickness;
        memberType = MemberType2D.Wall;
      }

      var speckleMaterial = GetStructuralMaterial(structMaterial);
      prop.material = speckleMaterial;

      prop.name = structuralElement.Name;
      //prop.type = memberType;
      //prop.analysisType = Structural.AnalysisType2D.Shell;
      prop.thickness = thickness;

      speckleElement2D.property = prop;

      GetAllRevitParamsAndIds(speckleElement2D, revitSurface);

      //speckleElement2D.displayMesh = GetElementDisplayMesh(Doc.GetElement(revitSurface.GetElementId()),
      // new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false });

      return speckleElement2D;
    }

    private List<Polycurve> GetOpenings(AnalyticalPanel revitSurface)
    {
      var openings = new List<Polycurve>();
      foreach (var openingId in revitSurface.GetAnalyticalOpeningsIds())
      {
        if (revitSurface.Document.GetElement(openingId) is not AnalyticalOpening opening) continue;

        var curveLoop = opening.GetOuterContour();
        openings.Add(CurveLoopToSpeckle(curveLoop, revitSurface.Document));
      }
      return openings;
    }
#endif
  }

}
