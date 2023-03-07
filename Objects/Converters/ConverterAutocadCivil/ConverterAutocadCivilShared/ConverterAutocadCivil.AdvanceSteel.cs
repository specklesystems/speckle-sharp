#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Models;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Acad = Autodesk.AutoCAD.Geometry;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Objects.BuiltElements.AdvanceSteel;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using Interval = Objects.Primitive.Interval;
using Polycurve = Objects.Geometry.Polycurve;
using Curve = Objects.Geometry.Curve;
using Featureline = Objects.BuiltElements.Featureline;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Brep = Objects.Geometry.Brep;
using Mesh = Objects.Geometry.Mesh;
using Pipe = Objects.BuiltElements.Pipe;
using Plane = Objects.Geometry.Plane;
using Polyline = Objects.Geometry.Polyline;
using Profile = Objects.BuiltElements.Profile;
using Spiral = Objects.Geometry.Spiral;
using SpiralType = Objects.Geometry.SpiralType;
using Station = Objects.BuiltElements.Station;
using Structure = Objects.BuiltElements.Structure;
using Objects.Other;
using ASBeam = Autodesk.AdvanceSteel.Modelling.Beam;
using ASPlate = Autodesk.AdvanceSteel.Modelling.Plate;
using ASBoltPattern = Autodesk.AdvanceSteel.Modelling.BoltPattern;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.CADLink.Database;
using CADObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using Autodesk.AdvanceSteel.DocumentManagement;
using Autodesk.AdvanceSteel.Geometry;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using System.Security.Cryptography;
using System.Collections;
using Autodesk.AdvanceSteel.Modeler;
using Objects.Geometry;
using Autodesk.AutoCAD.BoundaryRepresentation;
using MathNet.Spatial.Euclidean;
using MathPlane = MathNet.Spatial.Euclidean.Plane;
using TriangleNet.Geometry;
using TriangleVertex = TriangleNet.Geometry.Vertex;
using TriangleMesh = TriangleNet.Mesh;
using TriangleNet.Topology;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Modelling;
using Objects.BuiltElements;

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
          return true;
      }

      return false;
    }

    public Base ObjectASToSpeckle(DBObject @object, ApplicationObject reportObj, List<string> notes)
    {
      Base @base = null;

      void updateDescriptor(FilerObject filerObject)
      {
        reportObj.Update(descriptor: filerObject.GetType().ToString());
      }

      switch (@object.ObjectId.ObjectClass.DxfName)
      {
        case DxfNames.BEAM:
          ASBeam beam = GetFilerObjectByEntity<ASBeam>(@object);
          updateDescriptor(beam);
          return BeamToSpeckle(beam, notes);
        case DxfNames.PLATE:
          ASPlate plate = GetFilerObjectByEntity<ASPlate>(@object);
          updateDescriptor(plate);
          return PlateToSpeckle(plate, notes);
        case DxfNames.BOLT2POINTS:
        case DxfNames.BOLTCIRCULAR:
        case DxfNames.BOLTCORNER:
        case DxfNames.BOLTMID:
          ASBoltPattern bolt = GetFilerObjectByEntity<ASBoltPattern>(@object);
          updateDescriptor(bolt);
          return BoltToSpeckle(bolt, notes);
      }

      return @base;
    }

    private AdvanceSteelBeam BeamToSpeckle(ASBeam beam, List<string> notes)
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

    private AdvanceSteelPlate PlateToSpeckle(ASPlate plate, List<string> notes)
    {
      AdvanceSteelPlate advanceSteelPlate = new AdvanceSteelPlate();

      var units = ModelUnits;

      plate.GetBaseContourPolygon(0, out ASPoint3d[] ptsContour);

      advanceSteelPlate.outline = PolycurveToSpeckle(ptsContour);

      advanceSteelPlate.area = plate.GetPaintArea();

      SetDisplayValue(advanceSteelPlate, plate);

      SetUnits(advanceSteelPlate);

      return advanceSteelPlate;
    }

    private AdvanceSteelBolt BoltToSpeckle(ASBoltPattern bolt, List<string> notes)
    {
      AdvanceSteelBolt advanceSteelBolt = bolt is CircleScrewBoltPattern ? (AdvanceSteelBolt)new AdvanceSteelCircularBolt() : (AdvanceSteelBolt)new AdvanceSteelRectangularBolt();

      var units = ModelUnits;

      SetDisplayValue(advanceSteelBolt, bolt);

      SetUnits(advanceSteelBolt);

      return advanceSteelBolt;
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
