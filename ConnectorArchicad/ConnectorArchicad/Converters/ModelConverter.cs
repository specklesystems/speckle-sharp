using System;
using System.Collections.Generic;
using System.Linq;
using Archicad.Converters;
using Archicad.Model;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;

namespace Archicad.Operations
{
  public static class ModelConverter
  {
    public static List<Mesh> MeshesToSpeckle(MeshModel meshModel)
    {
      var materials = meshModel.materials.Select(MaterialToSpeckle).ToList();
      var meshes = materials.Select(m => new Mesh { units = Units.Meters, ["renderMaterial"] = m }).ToList();
      var vertCount = new int[materials.Count];

      foreach (var poly in meshModel.polygons)
      {
        var meshIndex = poly.material;
        meshes[meshIndex].vertices.AddRange(poly.pointIds.SelectMany(id => FlattenPoint(meshModel.vertices[id]))
          .ToList());
        meshes[meshIndex].faces
          .AddRange(PolygonToSpeckle(poly, vertCount[meshIndex]));
        vertCount[meshIndex] += poly.pointIds.Count;
      }

      return meshes;
    }

    public static MeshModel MeshToNative(IEnumerable<Mesh> meshes)
    {
      var meshModel = new MeshModel();
      var enumerable = meshes as Mesh[] ?? meshes.ToArray();
      foreach (var mesh in enumerable)
      {
        int vertexOffset = meshModel.vertices.Count;
        var polygons = PolygonToNative(mesh.faces);
        polygons.ForEach(p => p.pointIds = p.pointIds.Select(l => l + vertexOffset).ToList());

        meshModel.vertices.AddRange(mesh.GetPoints().Select(p => Utils.PointToNative(p)));
        meshModel.polygons.AddRange(polygons);
      }

      return meshModel;
    }

    private static IEnumerable<double> FlattenPoint(MeshModel.Vertex vertex)
    {
      return new List<double> { vertex.x, vertex.y, vertex.z };
    }

    private static IEnumerable<int> PolygonToSpeckle(MeshModel.Polygon polygon, int offset = 0)
    {
      // wait until ngons are supported in the viewer
      // var n = polygon.pointIds.Count;
      // if ( n < 3 ) n += 3;
      // var vertexIds = new List<int> { n };

      var vertexIds = new List<int> { polygon.pointIds.Count == 3 ? 0 : 1 };
      vertexIds.AddRange(Enumerable.Range(0, polygon.pointIds.Count).Select(r => r + offset));

      return vertexIds;
    }

    private static List<MeshModel.Polygon> PolygonToNative(List<int> polygon)
    {
      var result = new List<MeshModel.Polygon>();

      for (var i = 0; i < polygon.Count; i++)
      {
        var n = polygon[i];
        if (n < 3) n += 3;
        result.Add(new MeshModel.Polygon { pointIds = polygon.GetRange(i + 1, n) });
        i += n;
      }

      return result;
    }

    private static RenderMaterial MaterialToSpeckle(Model.MeshModel.Material material)
    {
      System.Drawing.Color ConvertColor(Model.MeshModel.Material.Color color)
      {
        // In AC the Colors are encoded in ushort
        return System.Drawing.Color.FromArgb(color.red / 256, color.green / 256, color.blue / 256);
      }

      return new RenderMaterial
      {
        name = material.name,
        diffuse = ConvertColor(material.ambientColor).ToArgb(),
        emissive = ConvertColor(material.emissionColor).ToArgb(),
        opacity = 1.0 - material.transparency / 100.0
      };
    }
  }
}
