using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Objects.Other;

namespace Speckle.Converters.RevitShared.Helpers;

// POC: probably interface out
public sealed class MeshBuildHelper
{
  //Lazy initialised Dictionary of Revit material (hash) -> Speckle material
  private readonly Dictionary<int, RenderMaterial?> _materialMap = new();

  public RenderMaterial? GetOrCreateMaterial(DB.Material revitMaterial)
  {
    int hash = Hash(revitMaterial); //Key using the hash as we may be given several instances with identical material properties
    if (_materialMap.TryGetValue(hash, out RenderMaterial? m))
    {
      return m;
    }

    var material = RenderMaterialToSpeckle(revitMaterial);
    _materialMap.Add(hash, material);
    return material;
  }

  // POC: cannot see how the hash is guaranteed to be unique
  // surely we could store hash as a unsigned long and use bit shifting
  // Extension method preferred
  private static int Hash(DB.Material mat) =>
    mat.Transparency ^ mat.Color.Red ^ mat.Color.Green ^ mat.Color.Blue ^ mat.Smoothness ^ mat.Shininess;

  //Lazy initialised Dictionary of revit material (hash) -> Speckle Mesh
  private readonly Dictionary<int, Mesh> _meshMap = new();

  // POC: nullability needs checking
  // what is this meant to be doing?
  // what is the empty mesh and the material hash?
  // feels like we should be caching materials but...
  // why did we choose to cache this material?
  // what is the relevance of the units here?
  // probably we should have some IMaterialCache
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

    // POC: not sure we should be pulling in System.Drawing -
    // maybe this isn't a problem as it's part of the netstandard Fwk
    // ideally we'd have serialiser of our own colour class, i.e. to serialise to an uint
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
