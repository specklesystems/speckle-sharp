using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public Mesh GetElementMesh(DB.Element element, List<DB.Element> subElements = null)
    {
      Mesh mesh = new Mesh();

      var allSolids = GetElementSolids(element, opt: new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true });
      if (!allSolids.Any()) //it's a mesh!
      {
        var geom = element.get_Geometry(new Options());
        mesh = GetMesh(geom);
      }
      else
      {
        if (subElements != null)
        {
          foreach (var sb in subElements)
          {
            allSolids.AddRange(GetElementSolids(sb));
          }
        }

        (mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(allSolids);
      }

      mesh.units = ModelUnits;
      return mesh;
    }

    /// <summary>
    /// Returns a mesh representing the provided element, if possible.
    /// </summary>
    /// <param name="elem">Element you want a mesh from.</param>
    /// <param name="opt">The view options to use</param>
    /// <param name="useOriginGeom4FamilyInstance">Whether to refer to the orignal geometry of the family (if it's a family).</param>
    /// <returns></returns>
    public Mesh GetElementDisplayMesh(DB.Element elem, Options opt = null, bool useOriginGeom4FamilyInstance = false)
    {
      var mesh = new Mesh();
      mesh.units = ModelUnits;

      List<Solid> solids = new List<Solid>();

      if (elem is Group g)
      {
        foreach (var id in g.GetMemberIds())
        {
          var subSolids = GetElementSolids(Doc.GetElement(id), opt, useOriginGeom4FamilyInstance);
          if (subSolids != null && subSolids.Any())
            solids.AddRange(subSolids);
        }
      }
      else
        solids = GetElementSolids(elem, opt, useOriginGeom4FamilyInstance);


      (mesh.faces, mesh.vertices) = GetFaceVertexArrFromSolids(solids);
      return mesh;
    }

    /// <summary>
    /// Gets all the solids from an element (digs into them too!). see: https://forums.autodesk.com/t5/revit-api-forum/getting-beam-column-and-wall-geometry/td-p/8138893
    /// </summary>
    /// <param name="elem"></param>
    /// <param name="opt"></param>
    /// <param name="useOriginGeom4FamilyInstance"></param>
    /// <returns></returns>
    public List<Solid> GetElementSolids(DB.Element elem, Options opt = null, bool useOriginGeom4FamilyInstance = false)
    {
      if (null == elem)
      {
        return null;
      }
      if (null == opt)
      {
        opt = new Options();
      }

      List<Solid> solids = new List<Solid>();
      GeometryElement gElem = null;
      try
      {
        if (useOriginGeom4FamilyInstance && elem is Autodesk.Revit.DB.FamilyInstance)
        {
          // we transform the geometry to instance coordinate to reflect actual geometry
          Autodesk.Revit.DB.FamilyInstance fInst = elem as Autodesk.Revit.DB.FamilyInstance;
          gElem = fInst.GetOriginalGeometry(opt);
          Transform trf = fInst.GetTransform();
          if (!trf.IsIdentity)
            gElem = gElem.GetTransformed(trf);
        }
        else
        {
          gElem = elem.get_Geometry(opt);
        }

        if (null == gElem)
        {
          return null;
        }
        IEnumerator<GeometryObject> gIter = gElem.GetEnumerator();
        gIter.Reset();
        while (gIter.MoveNext())
        {
          solids.AddRange(GetSolids(gIter.Current));
        }
      }
      catch (Exception ex)
      {
        // In Revit, sometime get the geometry will failed.
        string error = ex.Message;
      }
      return solids;
    }

    private Mesh GetMesh(GeometryElement geom)
    {
      var speckleMesh = new Mesh();
      foreach (var element in geom)
      {
        if (element is DB.Mesh)
        {
          var mesh = (DB.Mesh)element;

          foreach (var vert in mesh.Vertices)
          {
            var vertex = PointToSpeckle(vert);
            speckleMesh.vertices.AddRange(new double[] { vertex.x, vertex.y, vertex.z });
          }

          for (int i = 0; i < mesh.NumTriangles; i++)
          {
            var triangle = mesh.get_Triangle(i);
            var A = triangle.get_Index(0);
            var B = triangle.get_Index(1);
            var C = triangle.get_Index(2);
            speckleMesh.faces.Add(0);
            speckleMesh.faces.AddRange(new int[] { (int)A, (int)B, (int)C });
          }
        }
      }
      speckleMesh.units = ModelUnits;
      return speckleMesh;
    }

    /// <summary>
    /// Extracts solids from a geometry object. see: https://forums.autodesk.com/t5/revit-api-forum/getting-beam-column-and-wall-geometry/td-p/8138893
    /// </summary>
    /// <param name="gObj"></param>
    /// <returns></returns>
    private List<Solid> GetSolids(GeometryObject gObj)
    {
      List<Solid> solids = new List<Solid>();
      if (gObj is Solid) // already solid
      {
        Solid solid = gObj as Solid;
        if (solid.Faces.Size > 0 && Math.Abs(solid.Volume) > 0) // skip invalid solid
          solids.Add(gObj as Solid);
      }
      else if (gObj is GeometryInstance) // find solids from GeometryInstance
      {
        IEnumerator<GeometryObject> gIter2 = (gObj as GeometryInstance).GetInstanceGeometry().GetEnumerator();
        gIter2.Reset();
        while (gIter2.MoveNext())
        {
          solids.AddRange(GetSolids(gIter2.Current));
        }
      }
      else if (gObj is GeometryElement) // find solids from GeometryElement
      {
        IEnumerator<GeometryObject> gIter2 = (gObj as GeometryElement).GetEnumerator();
        gIter2.Reset();
        while (gIter2.MoveNext())
        {
          solids.AddRange(GetSolids(gIter2.Current));
        }
      }
      return solids;
    }

    /// <summary>
    /// Returns a merged face and vertex array for the group of solids passed in that can be used to set them in a speckle mesh or any object that inherits from a speckle mesh.
    /// </summary>
    /// <param name="solids"></param>
    /// <returns></returns>
    public (List<int>, List<double>) GetFaceVertexArrFromSolids(IEnumerable<Solid> solids)
    {
      var faceArr = new List<int>();
      var vertexArr = new List<double>();
      var prevVertCount = 0;

      if (solids == null) return (faceArr, vertexArr);

      foreach (var solid in solids)
      {
        foreach (Face face in solid.Faces)
        {
          var m = face.Triangulate();
          var points = m.Vertices;

          foreach (var vert in m.Vertices)
          {
            var vertex = PointToSpeckle(vert);
            vertexArr.AddRange(new double[] { vertex.x, vertex.y, vertex.z });
          }

          for (int i = 0; i < m.NumTriangles; i++)
          {
            var triangle = m.get_Triangle(i);

            faceArr.Add(0); // TRIANGLE flag
            faceArr.Add((int)triangle.get_Index(0) + prevVertCount);
            faceArr.Add((int)triangle.get_Index(1) + prevVertCount);
            faceArr.Add((int)triangle.get_Index(2) + prevVertCount);
          }
          prevVertCount += m.Vertices.Count;
        }
      }

      return (faceArr, vertexArr);
    }
  }
}