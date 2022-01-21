using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Other;

using DB = Autodesk.Revit.DB;

using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<Mesh> GetElementMesh(DB.Element element, List<DB.Element> subElements = null)
    {
      var allSolids = GetElementSolids(element, opt: new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true });
      if (!allSolids.Any()) //it's a mesh!
      {
        var geom = element.get_Geometry(new Options());
        return GetMeshes(geom);
      }

      if (subElements != null)
      {
        foreach (var sb in subElements)
        {
          allSolids.AddRange(GetElementSolids(sb));
        }
      }
      
      return GetMeshesFromSolids(allSolids);
      
    }

    /// <summary>
    /// Returns a mesh representing the provided element, if possible.
    /// </summary>
    /// <param name="elem">Element you want a mesh from.</param>
    /// <param name="opt">The view options to use</param>
    /// <param name="useOriginGeom4FamilyInstance">Whether to refer to the orignal geometry of the family (if it's a family).</param>
    /// <returns></returns>
    public List<Mesh> GetElementDisplayMesh(DB.Element elem, Options opt = null, bool useOriginGeom4FamilyInstance = false)
    {
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
      
      return GetMeshesFromSolids(solids);
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
          DB.Transform trf = fInst.GetTransform();
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



    
    

    private List<Mesh> GetMeshes(GeometryElement geom)
    {
      MeshBuildHelper buildHelper = new MeshBuildHelper();

      foreach (var element in geom)
      {
        if (element is DB.Mesh mesh)
        {
          var revitMaterial = Doc.GetElement(mesh.MaterialElementId) as Material;
          Mesh speckleMesh = buildHelper.GetOrCreateMesh(revitMaterial, ModelUnits);

          int faceIndexOffset = speckleMesh.vertices.Count;
          
          speckleMesh.vertices.Capacity += mesh.Vertices.Count * 3;
          foreach (XYZ vert in mesh.Vertices)
          {
            var (x, y, z) = PointToSpeckle(vert);
            speckleMesh.vertices.AddRange(new double[] { x, y, z });
          }
          
          speckleMesh.faces.Capacity += mesh.NumTriangles * 4;
          for (int i = 0; i < mesh.NumTriangles; i++)
          {
            var triangle = mesh.get_Triangle(i);
            speckleMesh.faces.Add(0); //Triangle indicator
            speckleMesh.faces.Add( (int)(faceIndexOffset + triangle.get_Index(0)) );
            speckleMesh.faces.Add( (int)(faceIndexOffset + triangle.get_Index(1)) );
            speckleMesh.faces.Add( (int)(faceIndexOffset + triangle.get_Index(2)) );
          }
        }
      }
      
      return buildHelper.GetAllValidMeshes();
    }
    
    private class MeshBuildHelper
    {
      //Lazy initialised Dictionary of Revit material (hash) -> Speckle material
      private readonly Dictionary<int, RenderMaterial> materialMap = new Dictionary<int, RenderMaterial>();
      public RenderMaterial GetOrCreateMaterial(Material revitMaterial)
      {
        if (revitMaterial == null) return null;
        
        int hash = Hash(revitMaterial); //Key using the hash as we may be given several instances with identical material properties
        if (materialMap.TryGetValue(hash, out RenderMaterial m))
        {
          return m;
        }
        var material = RenderMaterialToNative(revitMaterial);
        materialMap.Add(hash, material);
        return material;
      }
      
      private static int Hash(Material mat)
        => mat.Transparency ^ mat.Color.Red ^ mat.Color.Green ^ mat.Color.Blue ^ mat.Smoothness ^ mat.Shininess;
      
      //Mesh to use for null materials (because dictionary keys can't be null)
      private Mesh nullMesh;
      //Lazy initialised Dictionary of revit material (hash) -> Speckle Mesh
      private readonly Dictionary<int, Mesh> meshMap = new Dictionary<int, Mesh>();
      public Mesh GetOrCreateMesh(Material mat, string units)
      {
        if (mat == null) return nullMesh ??= new Mesh {units = units};

        int materialHash = Hash(mat);
        if (meshMap.TryGetValue(materialHash, out Mesh m)) return m;
        
        var mesh = new Mesh {
          ["renderMaterial"] = GetOrCreateMaterial(mat),
          units = units
        };
        meshMap.Add(materialHash, mesh);
        return mesh;
      }

      public List<Mesh> GetAllMeshes()
      {
        List<Mesh> meshes = meshMap.Values.ToList();
        if(nullMesh != null) meshes.Add(nullMesh);
        return meshes;
      }
      
      public List<Mesh> GetAllValidMeshes() => GetAllMeshes().FindAll(m => m.vertices.Count > 0 && m.faces.Count > 0);
      
    }

    /// <summary>
    /// Extracts solids from a geometry object. see: https://forums.autodesk.com/t5/revit-api-forum/getting-beam-column-and-wall-geometry/td-p/8138893
    /// </summary>
    /// <param name="gObj"></param>
    /// <returns></returns>
    private List<Solid> GetSolids(GeometryObject gObj)
    {
      List<Solid> solids = new List<Solid>();

      void Iterate(GeometryObject geometryObject)
      {
        if (geometryObject is Solid gSolid) // already solid
        {
          if (gSolid.Faces.Size > 0 && Math.Abs(gSolid.Volume) > 0) // skip invalid solid
            solids.Add(gSolid);
        }
        else if (geometryObject is GeometryInstance gInstance) // find solids from GeometryInstance
        {
          foreach (var g in gInstance.GetInstanceGeometry()) Iterate(g);
        }
        else if (geometryObject is GeometryElement gElement) // find solids from GeometryElement
        {
          foreach (var g in gElement) Iterate(g);
        }
      }

      Iterate(gObj);

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

      if (solids == null) return (faceArr, vertexArr);

      foreach (var solid in solids)
      {
        foreach (Face face in solid.Faces)
        {
          GetFaceVertexArrFromSolid(face, faceArr, vertexArr);
        }
      }

      return (faceArr, vertexArr);
    }
    
    public List<Mesh> GetMeshesFromSolids(IEnumerable<Solid> solids)
    {
      MeshBuildHelper meshBuildHelper = new MeshBuildHelper();

      foreach (Solid solid in solids)
      {
        foreach (Face face in solid.Faces)
        {
          Material faceMaterial = Doc.GetElement(face.MaterialElementId) as Material;
          Mesh m = meshBuildHelper.GetOrCreateMesh(faceMaterial, ModelUnits);
          GetFaceVertexArrFromSolid(face, m.faces, m.vertices);
        }
      }

      return meshBuildHelper.GetAllValidMeshes();
    }
    
    


    private void GetFaceVertexArrFromSolid(Face face, List<int> faces, List<double> vertices)
    {
      int vertOffset = vertices.Count / 3;
      var m = face.Triangulate();

      foreach (var vert in m.Vertices)
      {
        var (x, y, z) = PointToSpeckle(vert);
        vertices.AddRange(new double[] { x, y, z });
      }

      for (int i = 0; i < m.NumTriangles; i++)
      {
        var triangle = m.get_Triangle(i);

        faces.Add(0); // TRIANGLE flag
        faces.Add((int)triangle.get_Index(0) + vertOffset);
        faces.Add((int)triangle.get_Index(1) + vertOffset);
        faces.Add((int)triangle.get_Index(2) + vertOffset);
      }
    }
  }
}