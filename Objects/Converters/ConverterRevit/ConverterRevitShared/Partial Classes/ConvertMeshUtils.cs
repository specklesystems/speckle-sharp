
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    /// <summary>
    /// Retreives the meshes on an element to use as the speckle displayvalue
    /// </summary>
    /// <param name="element"></param>
    /// <param name="doNotTransformWithReferencePoint">For instances, determines if the retrieved geometry should be transformed by the selected document reference point.</param>
    /// <returns></returns>
    /// <remarks>
    /// See https://www.revitapidocs.com/2023/e0f15010-0e19-6216-e2f0-ab7978145daa.htm for a full Geometry Object inheritance
    /// </remarks>
    public List<Mesh> GetElementDisplayValue(DB.Element element, Options options = null, bool doNotTransformWithReferencePoint = false)
    {
      var displayMeshes = new List<Mesh>();

      // test if the element is a group first
      if (element is Group g)
      {
        foreach (var id in g.GetMemberIds())
        {
          var groupMeshes = GetElementDisplayValue(element.Document.GetElement(id), options, doNotTransformWithReferencePoint);
          displayMeshes.AddRange(groupMeshes);
        }
        return displayMeshes;
      }

      options ??= new Options();
      var geom = element.get_Geometry(options);

      // retrieves all meshes and solids from a geometry element
      var solids = new List<Solid>();
      var meshes = new List<DB.Mesh>();
      SortGeometry(geom);
      void SortGeometry(GeometryElement geom)
      {
        foreach (GeometryObject geomObj in geom)
        {
          switch (geomObj)
          {
            case Solid solid:
              if (solid.Faces.Size > 0 && Math.Abs(solid.SurfaceArea) > 0) // skip invalid solid
                solids.Add(solid);
              break;
            case DB.Mesh mesh:
              meshes.Add(mesh);
              break;
            case GeometryInstance instance:
              var instanceGeo = doNotTransformWithReferencePoint ? instance.GetSymbolGeometry() : instance.GetInstanceGeometry();
              SortGeometry(instanceGeo);
              break;
            case GeometryElement element:
              SortGeometry(element);
              break;
          }
        }
      }

      // convert meshes and solids
      displayMeshes.AddRange(ConvertMeshesByRenderMaterial(meshes, element.Document, doNotTransformWithReferencePoint));
      displayMeshes.AddRange(ConvertSolidsByRenderMaterial(solids, element.Document, doNotTransformWithReferencePoint));

      return displayMeshes;
    }

    /// <summary>
    /// Given a collection of <paramref name="meshes"/>, will create one <see cref="Mesh"/> per distinct <see cref="DB.Material"/>
    /// </summary>
    /// <param name="meshes"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public List<Mesh> ConvertMeshesByRenderMaterial(List<DB.Mesh> meshes, Document d, bool doNotTransformWithReferencePoint = false)
    {
      MeshBuildHelper buildHelper = new MeshBuildHelper();

      foreach (var mesh in meshes)
      {
        var revitMaterial = d.GetElement(mesh.MaterialElementId) as DB.Material;
        Mesh speckleMesh = buildHelper.GetOrCreateMesh(revitMaterial, ModelUnits);
        ConvertMeshData(mesh, speckleMesh.faces, speckleMesh.vertices, d, doNotTransformWithReferencePoint);
      }

      return buildHelper.GetAllValidMeshes();
    }

    /// <summary>
    /// Given a collection of <paramref name="solids"/>, will create one <see cref="Mesh"/> per distinct <see cref="DB.Material"/>
    /// </summary>
    /// <param name="solids"></param>
    /// <param name="d"></param>
    /// <returns></returns>
    public List<Mesh> ConvertSolidsByRenderMaterial(IEnumerable<Solid> solids, Document d, bool doNotTransformWithReferencePoint = false)
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
          if (mesh == null) continue;
          numberOfVertices += mesh.Vertices.Count * 3;
          numberOfFaces += mesh.NumTriangles * 4;
        }

        meshData.Key.faces.Capacity = numberOfFaces;
        meshData.Key.vertices.Capacity = numberOfVertices;
        foreach (DB.Mesh mesh in meshData.Value)
        {
          if (mesh == null) continue;
          ConvertMeshData(mesh, meshData.Key.faces, meshData.Key.vertices, d, doNotTransformWithReferencePoint);
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
    private void ConvertMeshData(DB.Mesh mesh, List<int> faces, List<double> vertices, Document doc, bool doNotTransformWithReferencePoint = false)
    {
      int faceIndexOffset = vertices.Count / 3;

      foreach (var vert in mesh.Vertices)
      {
        var (x, y, z) = PointToSpeckle(vert, doc, null, doNotTransformWithReferencePoint);
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

    #region old display value mesh methods: to be replaced by the `GetElementDisplayValue()`

    public List<Mesh> GetElementMesh(DB.Element element)
    {
      var allSolids = GetElementSolids(element, opt: new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true });
      if (allSolids.Any())
      {
        return ConvertSolidsByRenderMaterial(allSolids, element.Document);
      }
      else //it's a mesh!
      {
        var geom = element.get_Geometry(new Options());
        return GetMeshes(geom, element.Document);
      }
    }

    /// <summary>
    /// Returns a mesh representing the provided element, if possible.
    /// </summary>
    /// <param name="elem">Element you want a mesh from.</param>
    /// <param name="opt">The view options to use</param>
    /// <param name="useOriginGeom4FamilyInstance">Whether to refer to the orignal geometry of the family (if it's a family).</param>
    /// <returns></returns>
    public List<Mesh> GetElementDisplayMesh(DB.Element elem, Options opt = null, bool useOriginGeom4FamilyInstance = false, DB.Transform adjustedTransform = null)
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

      return ConvertSolidsByRenderMaterial(solids, elem.Document);
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

          ConvertMeshData(mesh, speckleMesh.faces, speckleMesh.vertices, d);
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
      meshes.AddRange(ConvertSolidsByRenderMaterial(allSolids, element.Document));

      return meshes;
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

    #endregion

  }
}
