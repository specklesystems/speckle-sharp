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
using ASPolyBeam = Autodesk.AdvanceSteel.Modelling.PolyBeam;
using ASPlate = Autodesk.AdvanceSteel.Modelling.Plate;
using ASBoltPattern = Autodesk.AdvanceSteel.Modelling.BoltPattern;
using ASSpecialPart = Autodesk.AdvanceSteel.Modelling.SpecialPart;
using ASGrating = Autodesk.AdvanceSteel.Modelling.Grating;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.CADLink.Database;
using CADObjectId = Autodesk.AutoCAD.DatabaseServices.ObjectId;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using Autodesk.AdvanceSteel.DocumentManagement;
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
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;

using ASPolyline3d = Autodesk.AdvanceSteel.Geometry.Polyline3d;
using ASCurve3d = Autodesk.AdvanceSteel.Geometry.Curve3d;
using ASLineSeg3d = Autodesk.AdvanceSteel.Geometry.LineSeg3d;
using ASCircArc3d = Autodesk.AdvanceSteel.Geometry.CircArc3d;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using ASVector3d = Autodesk.AdvanceSteel.Geometry.Vector3d;
using ASExtents = Autodesk.AdvanceSteel.Geometry.Extents;
using ASPlane = Autodesk.AdvanceSteel.Geometry.Plane;
using Autodesk.AdvanceSteel.DotNetRoots.DatabaseAccess;
using Autodesk.AdvanceSteel.Geometry;

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

      reportObj.Update(descriptor: filerObject.GetType().Name);

      dynamic dynamicObject = filerObject;
      AsteelObject advanceSteelObject = FilerObjectToSpeckle(dynamicObject, notes);

      SetUnits(advanceSteelObject);
      SetUserAttributes(filerObject as AtomicElement, advanceSteelObject);

      return advanceSteelObject;
    }

    private AsteelObject FilerObjectToSpeckle(ASPlate plate, List<string> notes)
    {
      AsteelPlate asteelPlate = new AsteelPlate();

      plate.GetBaseContourPolygon(0, out ASPoint3d[] ptsContour);

      asteelPlate.outline = PolycurveToSpeckle(ptsContour);

      asteelPlate.area = plate.GetPaintArea();

      SetDisplayValue(asteelPlate, plate);

      return asteelPlate;
    }

    private AsteelObject FilerObjectToSpeckle(ASBoltPattern bolt, List<string> notes)
    {
      AsteelBolt asteelBolt = bolt is CircleScrewBoltPattern ? (AsteelBolt)new AsteelCircularBolt() : (AsteelBolt)new AsteelRectangularBolt();

      SetDisplayValue(asteelBolt, bolt);

      return asteelBolt;
    }

    private AsteelObject FilerObjectToSpeckle(ASSpecialPart specialPart, List<string> notes)
    {
      AsteelSpecialPart asteelSpecialPart = new AsteelSpecialPart();

      SetDisplayValue(asteelSpecialPart, specialPart);

      return asteelSpecialPart;
    }

    private AsteelObject FilerObjectToSpeckle(ASGrating grating, List<string> notes)
    {
      AsteelGrating asteelGrating = new AsteelGrating();

      SetDisplayValue(asteelGrating, grating);

      SetUnits(asteelGrating);

      return asteelGrating;
    }

    private AsteelObject FilerObjectToSpeckle(FilerObject filerObject, List<string> notes)
    {
      throw new System.Exception("Advance Steel Object type conversion to Speckle not implemented");
    }

    private void SetDisplayValue(Base @base, AtomicElement atomicElement)
    {
      var modelerBody = atomicElement.GetModeler(Autodesk.AdvanceSteel.Modeler.BodyContext.eBodyContext.kMaxDetailed);

      @base["volume"] = modelerBody.Volume;
      @base["displayValue"] = new List<Mesh> { GetMeshFromModelerBody(modelerBody, atomicElement.GeomExtents) };
    }

    private Mesh GetMeshFromModelerBody(ModelerBody modelerBody, ASExtents extents)
    {
      modelerBody.getBrepInfo(out var verticesAS, out var facesInfo);

      IEnumerable<Point3D> vertices = verticesAS.Select(x => PointToMath(x));

      List<double> vertexList = new List<double> { };
      List<int> facesList = new List<int> { };

      foreach (var faceInfo in facesInfo)
      {
        int faceIndexOffset = vertexList.Count / 3;
        var input = new Polygon();

        //Create coordinateSystemAligned with OuterContour
        var outerList = faceInfo.OuterContour.Select(x => vertices.ElementAt(x));

        if (outerList.Count() < 3)
          continue;

        CoordinateSystem coordinateSystemAligned = CreateCoordinateSystemAligned(outerList);

        input.Add(CreateContour(outerList, coordinateSystemAligned));

        if (faceInfo.InnerContours != null)
        {
          foreach (var listInnerContours in faceInfo.InnerContours)
          {
            var innerList = listInnerContours.Select(x => vertices.ElementAt(x));
            if (innerList.Count() < 3)
              continue;

            input.Add(CreateContour(innerList, coordinateSystemAligned), true);
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

    private CoordinateSystem CreateCoordinateSystemAligned(IEnumerable<Point3D> points)
    {
      var point1 = points.ElementAt(0);
      var point2 = points.ElementAt(1);

      //Centroid calculated to avoid non-collinear points
      var centroid = Point3D.Centroid(points);
      var plane = MathPlane.FromPoints(centroid, point1, point2);

      UnitVector3D vectorX = (centroid - point1).Normalize();
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

    private void SetUserAttributes(AtomicElement atomicElement, AsteelObject advanceSteelObject)
    {
      advanceSteelObject.UserAttributes = new Dictionary<int, string>();
      for (int i = 0; i < 10; i++)
      {
        string attribute = atomicElement.GetUserAttribute(i);

        if (!string.IsNullOrEmpty(attribute))
        {
          advanceSteelObject.UserAttributes.Add(i + 1, attribute);
        }
      }
    }
  }
}

#endif
