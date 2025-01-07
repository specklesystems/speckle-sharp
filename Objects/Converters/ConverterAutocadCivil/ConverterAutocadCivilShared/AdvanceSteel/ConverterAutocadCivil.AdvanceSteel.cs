#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Models;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AdvanceSteel.CADAccess;
using Autodesk.AdvanceSteel.Modeler;
using Autodesk.AdvanceSteel.ConstructionTypes;
using ASObjectId = Autodesk.AdvanceSteel.CADLink.Database.ObjectId;
using ASFilerObject = Autodesk.AdvanceSteel.CADAccess.FilerObject;
using ASExtents = Autodesk.AdvanceSteel.Geometry.Extents;
using Objects.BuiltElements.AdvanceSteel;
using Mesh = Objects.Geometry.Mesh;
using MathNet.Spatial.Euclidean;
using MathPlane = MathNet.Spatial.Euclidean.Plane;
using TriangleNet.Geometry;
using TriangleVertex = TriangleNet.Geometry.Vertex;
using TriangleMesh = TriangleNet.Mesh;
using TriangleNet.Topology;
using Autodesk.AdvanceSteel.DocumentManagement;
using static Autodesk.AdvanceSteel.DotNetRoots.Units.Unit;

namespace Objects.Converter.AutocadCivil;

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
      case DxfNames.SLAB:
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
    IAsteelObject asteelObject = FilerObjectToSpeckle(dynamicObject, notes);

    SetUserAttributesToSpeckle(filerObject as AtomicElement, asteelObject);

    SetAsteelObjectPropertiesToSpeckle(asteelObject, filerObject);

    //throw new System.Exception("Test");

    Base @base = asteelObject as Base;

    SetUnits(@base);

    @base["weight unit"] = UnitWeight;
    @base["area unit"] = UnitArea;
    @base["volume unit"] = UnitVolume;

    var objectHandleHierarchy = StructureUtils.GetObjectHandleHierarchy();
    if (objectHandleHierarchy.ContainsKey(filerObject.Handle))
    {
      @base["hierarchy"] = objectHandleHierarchy[filerObject.Handle];
    }

    return @base;
  }

  private void SetAsteelObjectPropertiesToSpeckle(IAsteelObject asteelObject, FilerObject filerObject)
  {
    var propsAsteelObject = new Base();

    try
    {
      var type = filerObject.GetType();
      propsAsteelObject["advance steel type"] = type.Name;

      IEnumerable<ASTypeData> listPropertySets = ASPropertiesCache.Instance.GetPropertiesSetsByType(type);

      foreach (ASTypeData typeData in listPropertySets)
      {
        var propsSpecific = new Base();
        propsAsteelObject[$"{typeData.Description} props"] = propsSpecific;

        foreach (var propItem in typeData.PropertiesSpecific)
        {
          if (CheckProperty(propItem.Value, filerObject, out object propValue))
          {
            propsSpecific[propItem.Key] = propValue;
          }
        }
      }
    }
    catch (System.Exception e)
    {
      return;
    }

    asteelObject.asteelProperties = propsAsteelObject;
  }

  private bool CheckProperty(ASProperty propInfo, object @object, out object value)
  {
    value = propInfo.EvaluateValue(@object);
    if (value is null)
    {
      return false;
    }

    if (propInfo.ValueType.IsPrimitive || propInfo.ValueType == typeof(decimal))
    {
      if (propInfo.UnitType.HasValue && value is double)
      {
        value = FromInternalUnits((double)value, propInfo.UnitType.Value);
      }

      return true;
    }

    if (propInfo.ValueType == typeof(string))
    {
      return !string.IsNullOrEmpty(value as string);
    }

    if (propInfo.ValueType.IsEnum)
    {
      value = value.ToString();
      return true;
    }

    value = ConvertValueToSpeckle(value, propInfo.UnitType, out var converted);

    return converted;
  }

  private IAsteelObject FilerObjectToSpeckle(FilerObject filerObject, List<string> notes)
  {
    throw new System.Exception("Advance Steel Object type conversion to Speckle not implemented");
  }

  private void SetDisplayValue(Base @base, AtomicElement atomicElement)
  {
    var modelerBody = atomicElement.GetModeler(Autodesk.AdvanceSteel.Modeler.BodyContext.eBodyContext.kMaxDetailed);

    @base["volume"] = FromInternalUnits(modelerBody.Volume, eUnitType.kVolume);
    @base["displayValue"] = new List<Mesh> { GetMeshFromModelerBody(modelerBody, atomicElement.GeomExtents) };
  }

  private Mesh GetMeshFromModelerBody(ModelerBody modelerBody, ASExtents extents)
  {
    modelerBody.getBrepInfo(out var verticesAS, out var facesInfo);

    IEnumerable<Point3D> vertices = verticesAS.Select(x => PointToMath(x));

    List<double> vertexList = new() { };
    List<int> facesList = new() { };

    foreach (var faceInfo in facesInfo)
    {
      int faceIndexOffset = vertexList.Count / 3;
      var input = new Polygon();

      //Create coordinateSystemAligned with OuterContour
      var outerList = faceInfo.OuterContour.Select(x => vertices.ElementAt(x));

      if (outerList.Count() < 3)
      {
        continue;
      }

      CoordinateSystem coordinateSystemAligned = CreateCoordinateSystemAligned(outerList);

      input.Add(CreateContour(outerList, coordinateSystemAligned));

      if (faceInfo.InnerContours != null)
      {
        foreach (var listInnerContours in faceInfo.InnerContours)
        {
          var innerList = listInnerContours.Select(x => vertices.ElementAt(x));
          if (innerList.Count() < 3)
          {
            continue;
          }

          input.Add(CreateContour(innerList, coordinateSystemAligned), true);
        }
      }

      var triangleMesh = (TriangleMesh)input.Triangulate();

      CoordinateSystem coordinateSystemInverted = coordinateSystemAligned.Invert();
      var verticesMesh = triangleMesh.Vertices.Select(x =>
        new Point3D(x.X, x.Y, 0).TransformBy(coordinateSystemInverted)
      );

      vertexList.AddRange(GetFlatCoordinates(verticesMesh));
      facesList.AddRange(GetFaceVertices(triangleMesh.Triangles, faceIndexOffset));
    }

    Mesh mesh = new(vertexList, facesList, units: ModelUnits);
    mesh.bbox = BoxToSpeckle(extents);

    return mesh;
  }

  private Contour CreateContour(IEnumerable<Point3D> points, CoordinateSystem coordinateSystemAligned)
  {
    var listTriangleVertex = points
      .Select(x => x.TransformBy(coordinateSystemAligned))
      .Select(x => new TriangleVertex(x.X, x.Y));
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

    CoordinateSystem fromCs = new(point1, vectorX, vectorY, vectorZ);
    CoordinateSystem toCs = new(Point3D.Origin, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis);
    return CoordinateSystem.CreateMappingCoordinateSystem(fromCs, toCs);
  }

  public static T GetFilerObjectByEntity<T>(DBObject @object)
    where T : FilerObject
  {
    ASObjectId idCadEntity = new(@object.ObjectId.OldIdPtr);
    ASObjectId idFilerObject = DatabaseManager.GetFilerObjectId(idCadEntity, false);
    if (idFilerObject.IsNull())
    {
      return null;
    }

    return DatabaseManager.Open(idFilerObject) as T;
  }

  private void SetUserAttributesToSpeckle(AtomicElement atomicElement, IAsteelObject asteelObject)
  {
    asteelObject.userAttributes = new Base();
    for (int i = 0; i < 10; i++)
    {
      string attribute = atomicElement.GetUserAttribute(i);

      if (!string.IsNullOrEmpty(attribute))
      {
        asteelObject.userAttributes[(i + 1).ToString()] = attribute;
      }
    }
  }
}

#endif
