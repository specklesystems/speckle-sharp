using System;
using System.Collections.Generic;
using System.Linq;
using Archicad.Converters;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Archicad.Operations
{
  public static class ModelConverter
  {
    #region --- Functions ---

    public static List<Mesh> MeshToSpeckle(Model.MeshModel meshModel)
    {
      return meshModel.polygons.Select(p => CreateMeshFromPolygon(meshModel.vertices, p, meshModel.materials[p.material])).ToList();
    }

    public static Model.MeshModel MeshToNative(Mesh mesh)
    {
      var conversionFactor = Units.GetConversionFactor(mesh.units, Units.Meters);

      return new Model.MeshModel
      {
        vertices = mesh.GetPoints().Select(p => Utils.PointToNative(p)).ToList(),
          polygons = ConvertPolygon(mesh.faces)
      };
    }

    public static Model.MeshModel MeshToNative(IEnumerable<Mesh> meshes)
    {
      Model.MeshModel meshModel = new Model.MeshModel();
      var enumerable = meshes as Mesh[ ] ?? meshes.ToArray();
      Console.WriteLine($">>> in mesh to native - converting {enumerable.Count()} meshes");
      foreach (Mesh mesh in enumerable)
      {
        int vertexOffset = meshModel.vertices.Count;
        List<Model.MeshModel.Polygon> polygons = ConvertPolygon(mesh.faces);
        polygons.ForEach(p => p.pointIds = p.pointIds.Select(l => l + vertexOffset).ToList());

        meshModel.vertices.AddRange(mesh.GetPoints().Select(p => Utils.PointToNative(p)));
        meshModel.polygons.AddRange(polygons);
      }

      return meshModel;
    }

    private static List<double> FlattenPoint(Model.MeshModel.Vertex vertex)
    {
      return new List<double> { vertex.x, vertex.y, vertex.z };
    }

    private static List<int> ConvertPolygon(Model.MeshModel.Polygon polygon)
    {
      List<int> vertexIds = new List<int> { polygon.pointIds.Count() == 3 ? 0 : 1 };
      vertexIds.AddRange(Enumerable.Range(0, polygon.pointIds.Count()));

      return vertexIds;
    }

    private static List<Model.MeshModel.Polygon> ConvertPolygon(List<int> polygon)
    {
      List<Model.MeshModel.Polygon> result = new List<Model.MeshModel.Polygon>();

      for (int i = 0; i < polygon.Count; i++)
      {
        int step = polygon[i] == 0 ? 3 : 4;
        result.Add(new Model.MeshModel.Polygon { pointIds = polygon.GetRange(i + 1, step) });
        i += step;
      }

      return result;
    }

    private static RenderMaterial ConvertMaterial(Model.MeshModel.Material material)
    {
      System.Drawing.Color ConvertColor(Model.MeshModel.Material.Color color)
      {
        // In AC the Colors are encoded in ushort
        return System.Drawing.Color.FromArgb(color.red / 256, color.green / 256, color.blue / 256);
      }

      return new RenderMaterial
      {
        diffuse = ConvertColor(material.ambientColor).ToArgb(),
          emissive = ConvertColor(material.emissionColor).ToArgb(),
          opacity = 1.0 - material.transparency / 100.0
      };
    }

    private static Mesh CreateMeshFromPolygon(IList<Model.MeshModel.Vertex> vertices, Model.MeshModel.Polygon polygon, Model.MeshModel.Material material)
    {
      List<double> points = polygon.pointIds.SelectMany(id => FlattenPoint(vertices[id])).ToList();
      List<int> polygons = ConvertPolygon(polygon);

      var mesh = new Mesh(points, polygons, units : Units.Meters)
      {
        ["renderMaterial"] = ConvertMaterial(material)
      };

      return mesh;
    }

    #endregion
  }
}
