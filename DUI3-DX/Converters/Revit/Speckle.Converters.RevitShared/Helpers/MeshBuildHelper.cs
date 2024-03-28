using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Other;

namespace Speckle.Converters.RevitShared.Helpers;

public sealed class MeshBuildHelper
{
  //Lazy initialised Dictionary of Revit material (hash) -> Speckle material
  private readonly Dictionary<int, RenderMaterial?> _materialMap = new();

  public RenderMaterial? GetOrCreateMaterial(DB.Material revitMaterial)
  {
    if (revitMaterial == null)
    {
      return null;
    }

    int hash = Hash(revitMaterial); //Key using the hash as we may be given several instances with identical material properties
    if (_materialMap.TryGetValue(hash, out RenderMaterial? m))
    {
      return m;
    }

    var material = RenderMaterialToSpeckle(revitMaterial);
    _materialMap.Add(hash, material);
    return material;
  }

  private static int Hash(DB.Material mat) =>
    mat.Transparency ^ mat.Color.Red ^ mat.Color.Green ^ mat.Color.Blue ^ mat.Smoothness ^ mat.Shininess;

  //Lazy initialised Dictionary of revit material (hash) -> Speckle Mesh
  private readonly Dictionary<int, Mesh> _meshMap = new();

  public Mesh GetOrCreateMesh(DB.Material mat, string units)
  {
    if (mat == null)
    {
      return new Mesh { units = units };
    }

    int materialHash = Hash(mat);
    if (_meshMap.TryGetValue(materialHash, out Mesh m))
    {
      return m;
    }

    var mesh = new Mesh { ["renderMaterial"] = GetOrCreateMaterial(mat), units = units };
    _meshMap.Add(materialHash, mesh);
    return mesh;
  }

  public List<Mesh> GetAllMeshes()
  {
    List<Mesh> meshes = _meshMap.Values?.ToList() ?? new List<Mesh>();

    return meshes;
  }

  public List<Mesh> GetAllValidMeshes() => GetAllMeshes().FindAll(m => m.vertices.Count > 0 && m.faces.Count > 0);

  public static RenderMaterial? RenderMaterialToSpeckle(DB.Material? revitMaterial)
  {
    if (revitMaterial == null)
    {
      return null;
    }

    RenderMaterial material =
      new()
      {
        name = revitMaterial.Name,
        opacity = 1 - revitMaterial.Transparency / 100d,
        //metalness = revitMaterial.Shininess / 128d, //Looks like these are not valid conversions
        //roughness = 1 - (revitMaterial.Smoothness / 100d),
        diffuse = System.Drawing.Color
          .FromArgb(revitMaterial.Color.Red, revitMaterial.Color.Green, revitMaterial.Color.Blue)
          .ToArgb()
      };

    return material;
  }
}
