using System.Collections.Generic;
using System.Linq;
using Objects.Other;
using DB = Autodesk.Revit.DB;
using Mesh = Objects.Geometry.Mesh;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  /// <summary>
  /// Helper class for a single <see cref="Objects.Geometry.Mesh"/> object for each <see cref="Autodesk.Revit.DB.Material"/>
  /// </summary>
  private class MeshBuildHelper
  {
    //Lazy initialised Dictionary of Revit material (hash) -> Speckle material
    private readonly Dictionary<int, RenderMaterial> materialMap = new();

    public RenderMaterial GetOrCreateMaterial(DB.Material revitMaterial)
    {
      if (revitMaterial == null)
      {
        return null;
      }

      int hash = Hash(revitMaterial); //Key using the hash as we may be given several instances with identical material properties
      if (materialMap.TryGetValue(hash, out RenderMaterial m))
      {
        return m;
      }

      var material = RenderMaterialToSpeckle(revitMaterial);
      materialMap.Add(hash, material);
      return material;
    }

    private static int Hash(DB.Material mat) =>
      mat.Transparency ^ mat.Color.Red ^ mat.Color.Green ^ mat.Color.Blue ^ mat.Smoothness ^ mat.Shininess;

    //Mesh to use for null materials (because dictionary keys can't be null)
    private Mesh nullMesh;

    //Lazy initialised Dictionary of revit material (hash) -> Speckle Mesh
    private readonly Dictionary<int, Mesh> meshMap = new();

    public Mesh GetOrCreateMesh(DB.Material mat, string units)
    {
      if (mat == null)
      {
        return nullMesh ??= new Mesh { units = units };
      }

      int materialHash = Hash(mat);
      if (meshMap.TryGetValue(materialHash, out Mesh m))
      {
        return m;
      }

      var mesh = new Mesh { ["renderMaterial"] = GetOrCreateMaterial(mat), units = units };
      meshMap.Add(materialHash, mesh);
      return mesh;
    }

    public List<Mesh> GetAllMeshes()
    {
      List<Mesh> meshes = meshMap.Values?.ToList() ?? new List<Mesh>();
      if (nullMesh != null)
      {
        meshes.Add(nullMesh);
      }

      return meshes;
    }

    public List<Mesh> GetAllValidMeshes() => GetAllMeshes().FindAll(m => m.vertices.Count > 0 && m.faces.Count > 0);
  }
}
