﻿#if ADVANCESTEEL2023
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.CADLink.Database;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.DocumentManagement;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modeler;
using Autodesk.AdvanceSteel.Modelling;
using Autodesk.AutoCAD.BoundaryRepresentation;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using MathNet.Spatial.Euclidean;
using Objects.BuiltElements;
using Objects.BuiltElements.AdvanceSteel;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using Acad = Autodesk.AutoCAD.Geometry;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using ASBeam = Autodesk.AdvanceSteel.Modelling.Beam;
using ASBoltPattern = Autodesk.AdvanceSteel.Modelling.BoltPattern;
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
using ASGrating = Autodesk.AdvanceSteel.Modelling.Grating;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using ASPlate = Autodesk.AdvanceSteel.Modelling.Plate;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using ASSpecialPart = Autodesk.AdvanceSteel.Modelling.SpecialPart;
using Brep = Objects.Geometry.Brep;
using CADObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
using Curve = Objects.Geometry.Curve;
using Featureline = Objects.BuiltElements.Featureline;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using MathPlane = MathNet.Spatial.Euclidean.Plane;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Profile = Objects.BuiltElements.Profile;
using Spiral = Objects.Geometry.Spiral;
using SpiralType = Objects.Geometry.SpiralType;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;
using TriangleMesh = TriangleNet.Mesh;
using TriangleVertex = TriangleNet.Geometry.Vertex;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    public bool CanConvertASToSpeckle(DBObject @object)
    {
      switch (@object.ObjectId.ObjectClass.DxfName)
      {
        case DxfNames.BEAM:
        case DxfNames.PLATE:
        case DxfNames.BOLT2POINTS:
        case DxfNames.BOLTCIRCULAR:
        case DxfNames.BOLTCORNER:
        case DxfNames.BOLTMID:
        case DxfNames.SPECIALPART:
        case DxfNames.GRATING:
          return true;
      }

      return false;
    }

    public Base ConvertASToSpeckle(DBObject @object, ApplicationObject reportObj, List<string> notes)
    {
      ASFilerObject filerObject = GetFilerObjectByEntity<ASFilerObject>(@object);

      if (filerObject == null)
      {
        throw new System.Exception($"Failed to find Advance Steel object ${@object.Handle.ToString()}.");
      }

      reportObj.Update(descriptor: filerObject.GetType().ToString());

      dynamic dynamicObject = filerObject;
      return FilerObjectToSpeckle(dynamicObject, notes);
    }

    private Base FilerObjectToSpeckle(ASBeam beam, List<string> notes)
    {
      AdvanceSteelBeam advanceSteelBeam = new AdvanceSteelBeam();

      var startPoint = beam.GetPointAtStart();
      var endPoint = beam.GetPointAtEnd();
      var units = ModelUnits;

      Point speckleStartPoint = PointToSpeckle(startPoint, units);
      Point speckleEndPoint = PointToSpeckle(endPoint, units);
      advanceSteelBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint, units);
      advanceSteelBeam.baseLine.length = speckleStartPoint.DistanceTo(speckleEndPoint);

      advanceSteelBeam.area = beam.GetPaintArea();

      SetDisplayValue(advanceSteelBeam, beam);

      SetUnits(advanceSteelBeam);

      return advanceSteelBeam;
    }

    private Base FilerObjectToSpeckle(ASPlate plate, List<string> notes)
    {
      AdvanceSteelPlate advanceSteelPlate = new AdvanceSteelPlate();

      plate.GetBaseContourPolygon(0, out ASPoint3d[] ptsContour);

      advanceSteelPlate.outline = PolycurveToSpeckle(ptsContour);

      advanceSteelPlate.area = plate.GetPaintArea();

      SetDisplayValue(advanceSteelPlate, plate);

      SetUnits(advanceSteelPlate);

      return advanceSteelPlate;
    }

    private Base FilerObjectToSpeckle(ASBoltPattern bolt, List<string> notes)
    {
      AdvanceSteelBolt advanceSteelBolt = bolt is CircleScrewBoltPattern ? (AdvanceSteelBolt)new AdvanceSteelCircularBolt() : (AdvanceSteelBolt)new AdvanceSteelRectangularBolt();

      SetDisplayValue(advanceSteelBolt, bolt);

      SetUnits(advanceSteelBolt);

      return advanceSteelBolt;
    }

    private Base FilerObjectToSpeckle(ASSpecialPart specialPart, List<string> notes)
    {
      AdvanceSteelSpecialPart advanceSteelSpecialPart = new AdvanceSteelSpecialPart();

      SetDisplayValue(advanceSteelSpecialPart, specialPart);

      SetUnits(advanceSteelSpecialPart);

      return advanceSteelSpecialPart;
    }

    private Base FilerObjectToSpeckle(ASGrating grating, List<string> notes)
    {
      AdvanceSteelGrating advanceSteelGrating = new AdvanceSteelGrating();

      SetDisplayValue(advanceSteelGrating, grating);

      SetUnits(advanceSteelGrating);

      return advanceSteelGrating;
    }

    private Base FilerObjectToSpeckle(FilerObject filerObject, List<string> notes)
    {
      throw new System.Exception("Advance Steel Object type conversion to Speckle not implemented");
    }

    private void SetDisplayValue(Base @base, AtomicElement atomicElement)
    {
      var modelerBody = atomicElement.GetModeler(Autodesk.AdvanceSteel.Modeler.BodyContext.eBodyContext.kMaxDetailed);

      @base["volume"] = modelerBody.Volume;
      @base["displayValue"] = new List<Mesh> { GetMeshFromModelerBody(modelerBody, atomicElement.GeomExtents) };
    }

    private Mesh GetMeshFromModelerBody(ModelerBody modelerBody, Extents extents)
    {
      modelerBody.getBrepInfo(out var verticesAS, out var facesInfo);

      IEnumerable<Point3D> vertices = verticesAS.Select(x => PointASToMath(x));

      List<double> vertexList = new List<double> { };
      List<int> facesList = new List<int> { };

      foreach (var faceInfo in facesInfo)
      {
        int faceIndexOffset = vertexList.Count / 3;
        var input = new Polygon();

        //Create coordinateSystemAligned with OuterContour
        var outerList = faceInfo.OuterContour.Select(x => vertices.ElementAt(x));
        CoordinateSystem coordinateSystemAligned = CreateCoordinateSystemAligned(outerList);

        input.Add(CreateContour(outerList, coordinateSystemAligned));

        if (faceInfo.InnerContours != null)
        {
          foreach (var listInnerContours in faceInfo.InnerContours)
          {
            input.Add(CreateContour(listInnerContours.Select(x => vertices.ElementAt(x)), coordinateSystemAligned), true);
          }
        }

        var triangleMesh = (TriangleMesh)input.Triangulate();

        CoordinateSystem coordinateSystemInverted = coordinateSystemAligned.Invert();
        var verticesMesh = triangleMesh.Vertices.Select(x => new Point3D(x.X, x.Y, 0).TransformBy(coordinateSystemInverted));

        vertexList.AddRange(GetFlatCoordinates(verticesMesh));
        facesList.AddRange(GetFaceVertices(triangleMesh.Triangles, faceIndexOffset));
      }

      Mesh mesh = new Mesh(vertexList, facesList, units: ModelUnits);
      mesh.bbox = BoxToSpeckle(extents);

      return mesh;
    }

    private Contour CreateContour(IEnumerable<Point3D> points, CoordinateSystem coordinateSystemAligned)
    {
      var listTriangleVertex = points.Select(x => x.TransformBy(coordinateSystemAligned)).Select(x => new TriangleVertex(x.X, x.Y));
      return new Contour(listTriangleVertex);
    }

    private IEnumerable<double> GetFlatCoordinates(IEnumerable<Point3D> verticesMesh)
    {
      foreach (var vertice in verticesMesh)
      {
        yield return vertice.X;
        yield return vertice.Y;
        yield return vertice.Z;
      }
    }

    private IEnumerable<int> GetFaceVertices(ICollection<Triangle> triangles, int faceIndexOffset)
    {
      foreach (var triangle in triangles)
      {
        yield return 3;
        yield return triangle.GetVertex(0).ID + faceIndexOffset;
        yield return triangle.GetVertex(1).ID + faceIndexOffset;
        yield return triangle.GetVertex(2).ID + faceIndexOffset;
      }
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

    private CoordinateSystem CreateCoordinateSystemAligned(IEnumerable<Point3D> points)
    {
      var point1 = points.ElementAt(0);
      var point2 = points.ElementAt(1);

      //Centroid calculated to avoid non-collinear points
      var centroid = Point3D.Centroid(points);
      var plane = MathPlane.FromPoints(point1, point2, centroid);

      UnitVector3D vectorX = (point2 - point1).Normalize();
      UnitVector3D vectorZ = plane.Normal;
      UnitVector3D vectorY = vectorZ.CrossProduct(vectorX);

      CoordinateSystem fromCs = new CoordinateSystem(point1, vectorX, vectorY, vectorZ);
      CoordinateSystem toCs = new CoordinateSystem(Point3D.Origin, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);
      return CoordinateSystem.CreateMappingCoordinateSystem(fromCs, toCs);

    }

    public static T GetFilerObjectByEntity<T>(DBObject @object) where T : FilerObject
    {
      ASObjectId idCadEntity = new ASObjectId(@object.ObjectId.OldIdPtr);
      ASObjectId idFilerObject = DatabaseManager.GetFilerObjectId(idCadEntity, false);
      if (idFilerObject.IsNull())
        return null;

      return DatabaseManager.Open(idFilerObject) as T;
    }
  }
}

#endif
