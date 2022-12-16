using Autodesk.Revit.DB;
using Objects.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<Mesh> GetElementMesh(DB.Element element)
    {
      var allSolids = GetElementSolids(element, opt: new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true });
      if (!allSolids.Any()) //it's a mesh!
      {
        var geom = element.get_Geometry(new Options());
        return GetMeshes(geom, element.Document);
      }

      return GetMeshesFromSolids(allSolids, element.Document);
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
          var subSolids = GetElementSolids(elem.Document.GetElement(id), opt, useOriginGeom4FamilyInstance);
          if (subSolids != null && subSolids.Any())
            solids.AddRange(subSolids);
        }
      }
      else
        solids = GetElementSolids(elem, opt, useOriginGeom4FamilyInstance);

      return GetMeshesFromSolids(solids, elem.Document);
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
      List<Solid> solids = new List<Solid>();

      if (null == elem) return solids;

      opt ??= new Options();

      GeometryElement gElem;
      try
      {
        if (useOriginGeom4FamilyInstance && elem is DB.FamilyInstance fInst)
        {
          // we transform the geometry to instance coordinate to reflect actual geometry
          gElem = fInst.GetOriginalGeometry(opt);
          DB.Transform trf = fInst.GetTransform();
          if (!trf.IsIdentity)
            gElem = gElem.GetTransformed(trf);
        }
        else
        {
          gElem = elem.get_Geometry(opt);
        }

        if (gElem == null) return solids;

        solids.AddRange(gElem.SelectMany(GetSolids));
      }
      catch (Exception ex)
      {
        // In Revit, sometime get the geometry will failed.
        string error = ex.Message;
      }
      return solids;
    }

    private List<Mesh> GetMeshes(GeometryElement geom, Document d)
    {
      MeshBuildHelper buildHelper = new MeshBuildHelper();

      foreach (var element in geom)
      {
        if (element is DB.Mesh mesh)
        {
          var revitMaterial = d.GetElement(mesh.MaterialElementId) as DB.Material;
          Mesh speckleMesh = buildHelper.GetOrCreateMesh(revitMaterial, ModelUnits);

          ConvertMeshData(mesh, speckleMesh.faces, speckleMesh.vertices);
        }
      }

      return buildHelper.GetAllValidMeshes();
    }

    /// <summary>
    /// Get meshes from fabrication parts which have different geometry hierarchy than other revit elements.
    /// </summary>
    /// <param name="element"></param>
    /// <param name="subElements"></param>
    /// <returns></returns>
    public List<Mesh> GetFabricationMeshes(Element element, List<Element> subElements = null)
    {
      //Search for solids on geometry element level
      var allSolids = GetElementSolids(element, opt: new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true });

      List<Mesh> meshes = new List<Mesh>();

      var geom = element.get_Geometry(new Options());
      if (geom == null)
        return null;

      foreach (GeometryInstance instance in geom)
      {
        //Get instance geometry from fabrication part geometry
        var symbolGeometry = instance.GetInstanceGeometry();

        //Get meshes
        var symbolMeshes = GetMeshes(symbolGeometry, element.Document);
        meshes.AddRange(symbolMeshes);

        //Get solids
        var symbolSolids = GetSolids(symbolGeometry);
        allSolids.AddRange(symbolSolids);
      }


      if (subElements != null)
        foreach (var sb in subElements)
          allSolids.AddRange(GetElementSolids(sb));

      //Convert solids to meshes
      meshes.AddRange(GetMeshesFromSolids(allSolids, element.Document));

      return meshes;
    }

    /// <summary>
    /// Helper class for a single <see cref="Objects.Geometry.Mesh"/> object for each <see cref="DB.Material"/>
    /// </summary>
    private class MeshBuildHelper
    {
      //Lazy initialised Dictionary of Revit material (hash) -> Speckle material
      private readonly Dictionary<int, RenderMaterial> materialMap = new Dictionary<int, RenderMaterial>();
      public RenderMaterial GetOrCreateMaterial(DB.Material revitMaterial)
      {
        if (revitMaterial == null) return null;

        int hash = Hash(revitMaterial); //Key using the hash as we may be given several instances with identical material properties
        if (materialMap.TryGetValue(hash, out RenderMaterial m))
        {
          return m;
        }
        var material = RenderMaterialToSpeckle(revitMaterial);
        materialMap.Add(hash, material);
        return material;
      }

      private static int Hash(DB.Material mat)
        => mat.Transparency ^ mat.Color.Red ^ mat.Color.Green ^ mat.Color.Blue ^ mat.Smoothness ^ mat.Shininess;

      //Mesh to use for null materials (because dictionary keys can't be null)
      private Mesh nullMesh;
      //Lazy initialised Dictionary of revit material (hash) -> Speckle Mesh
      private readonly Dictionary<int, Mesh> meshMap = new Dictionary<int, Mesh>();
      public Mesh GetOrCreateMesh(DB.Material mat, string units)
      {
        if (mat == null) return nullMesh ??= new Mesh { units = units };

        int materialHash = Hash(mat);
        if (meshMap.TryGetValue(materialHash, out Mesh m)) return m;

        var mesh = new Mesh
        {
          ["renderMaterial"] = GetOrCreateMaterial(mat),
          units = units
        };
        meshMap.Add(materialHash, mesh);
        return mesh;
      }

      public List<Mesh> GetAllMeshes()
      {
        List<Mesh> meshes = meshMap.Values.ToList();
        if (nullMesh != null) meshes.Add(nullMesh);
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
          if (gSolid.Faces.Size > 0 && Math.Abs(gSolid.SurfaceArea) > 0) // skip invalid solid
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
          ConvertMeshData(face.Triangulate(), faceArr, vertexArr);
        }
      }

      return (faceArr, vertexArr);
    }

    /// <summary>
    /// Given a collection of <paramref name="solids"/>, will create one <see cref="Mesh"/> per distinct <see cref="DB.Material"/>
    /// </summary>
    /// <param name="solids"></param>
    /// <returns></returns>
    public List<Mesh> GetMeshesFromSolids(IEnumerable<Solid> solids, Document d)
    {
      MeshBuildHelper meshBuildHelper = new MeshBuildHelper();
      
      var MeshMap = new Dictionary<Mesh, List<DB.Mesh>>();
      foreach (Solid solid in solids)
      {
        foreach (Face face in solid.Faces)
        {
          DB.Material faceMaterial = d.GetElement(face.MaterialElementId) as DB.Material;
          Mesh m = meshBuildHelper.GetOrCreateMesh(faceMaterial, ModelUnits);
          if (!MeshMap.ContainsKey(m))
          {
            MeshMap.Add(m, new List<DB.Mesh>());
          }
          MeshMap[m].Add(face.Triangulate());
        }
      }

      foreach (var meshData in MeshMap)
      {
        //It's cheaper to resize lists manually, since we would otherwise be resizing a lot!
        int numberOfVertices = 0;
        int numberOfFaces = 0;
        foreach (DB.Mesh mesh in meshData.Value)
        {
          numberOfVertices += mesh.Vertices.Count * 3;
          numberOfFaces += mesh.NumTriangles * 4;
        }

        meshData.Key.faces.Capacity = numberOfFaces;
        meshData.Key.vertices.Capacity = numberOfVertices;
        foreach (DB.Mesh mesh in meshData.Value)
        {
          ConvertMeshData(mesh, meshData.Key.faces, meshData.Key.vertices);
        }
      }
      
      return meshBuildHelper.GetAllValidMeshes();
    }


    /// <summary>
    /// Given <paramref name="mesh"/>, will convert and add triangle data to <paramref name="faces"/> and <paramref name="vertices"/>
    /// </summary>
    /// <param name="mesh">The revit mesh to convert</param>
    /// <param name="faces">The faces list to add to</param>
    /// <param name="vertices">The vertices list to add to</param>
    private void ConvertMeshData(DB.Mesh mesh, List<int> faces, List<double> vertices)
    {
      int faceIndexOffset = vertices.Count / 3;

      foreach (var vert in mesh.Vertices)
      {
        var (x, y, z) = PointToSpeckle(vert);
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(z);
      }

      for (int i = 0; i < mesh.NumTriangles; i++)
      {
        var triangle = mesh.get_Triangle(i);

        faces.Add(3); // TRIANGLE flag
        faces.Add((int)triangle.get_Index(0) + faceIndexOffset);
        faces.Add((int)triangle.get_Index(1) + faceIndexOffset);
        faces.Add((int)triangle.get_Index(2) + faceIndexOffset);
      }
    }

  }
}
