using System;
using System.Collections.Generic;
using System.Linq;
using Archicad.Converters;
using Archicad.Model;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Kits;
using static Archicad.Model.MeshModel;

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
      var mergedEdges = new Dictionary<Tuple<Vertex, Vertex>, Tuple<List<int>, List<Tuple<int, int>>>>();

      int polygonIndexCounter = 0;
      var vertexOffset = 0;

      var meshModel = new MeshModel();
      var enumerable = meshes as Mesh[] ?? meshes.ToArray();
      foreach (var mesh in enumerable)
      {
        meshModel.vertices.AddRange(mesh.GetPoints().Select(p => Utils.PointToNative(p)));

        var renderMaterial = mesh["renderMaterial"] as RenderMaterial;
        MeshModel.Material material = null;
        if (renderMaterial != null)
        {
          material = MaterialToNative(renderMaterial);
          meshModel.materials.Add(material);
        }

        for (var i = 0; i < mesh.faces.Count; ++i)
        {
          MeshModel.Polygon polygon = new MeshModel.Polygon();

          var n = mesh.faces[i];
          if (n < 3) n += 3;
          for (var j = i + 1; j <= i + n; ++j)
          {
            int vertexId = mesh.faces[j];
            Vertex vertex = Utils.PointToNative(mesh.GetPoint(vertexId));

            int nextVertexId = (j == i + n) ? mesh.faces[i + 1] : mesh.faces[j + 1];
            Vertex nextVertex = Utils.PointToNative(mesh.GetPoint(nextVertexId));

            vertexId += vertexOffset;
            nextVertexId += vertexOffset;
            polygon.pointIds.Add(vertexId);

            Tuple<Vertex, Vertex> forwardEdge = new Tuple<Vertex, Vertex>(vertex, nextVertex);
            Tuple<Vertex, Vertex> backwardEdge = new Tuple<Vertex, Vertex>(nextVertex, vertex);
            Tuple<List<int>, List<Tuple<int, int>>> edgesByPolygons;
            if (mergedEdges.TryGetValue(forwardEdge, out edgesByPolygons) || mergedEdges.TryGetValue(backwardEdge, out edgesByPolygons))
            {
              edgesByPolygons.Item1.Add(polygonIndexCounter);
              edgesByPolygons.Item2.Add(new Tuple<int, int>(vertexId, nextVertexId));
            }
            else
            {
              List<int> polygonList = new List<int>() { polygonIndexCounter };
              Tuple<int, int> edgeIds = new Tuple<int, int>(vertexId, nextVertexId);
              mergedEdges.Add(forwardEdge, new Tuple<List<int>, List<Tuple<int, int>>>(polygonList, new List<Tuple<int, int>>() { edgeIds }));
            }
          }

          if (material != null)
          {
            polygon.material = meshModel.materials.Count - 1;
          }

          meshModel.polygons.Add(polygon);
          ++polygonIndexCounter;

          i += n;
        }
        vertexOffset += mesh.VerticesCount;

        meshModel.ids.Add(mesh.id);
      }

      foreach (var mergedEdge in mergedEdges)
      {
        foreach(var edge in mergedEdge.Value.Item2)
        {
          if (mergedEdge.Value.Item1.Count == 1)  // show unwelded edges
          {
            meshModel.edges.Add(edge, EdgeStatus.VisibleEdge);
          }
          else if (mergedEdge.Value.Item1.Count > 2) // hide problematic edges
          {
            meshModel.edges.Add(edge, EdgeStatus.HiddenEdge);
          }
          else // show/hide welded edges based on the polygon normals
          {
            Vector normal1, normal2;

            MeshModel.Polygon polygon = meshModel.polygons[mergedEdge.Value.Item1[0]];
            Vector vertex1 = new Vector(Utils.VertexToPoint(meshModel.vertices[polygon.pointIds[0]]));
            Vector vertex2 = new Vector(Utils.VertexToPoint(meshModel.vertices[polygon.pointIds[1]]));
            Vector vertex3 = new Vector(Utils.VertexToPoint(meshModel.vertices[polygon.pointIds[2]]));
            normal1 = Vector.CrossProduct(vertex1 - vertex2, vertex2 - vertex3);

            polygon = meshModel.polygons[mergedEdge.Value.Item1[1]];
            vertex1 = new Vector(Utils.VertexToPoint(meshModel.vertices[polygon.pointIds[0]]));
            vertex2 = new Vector(Utils.VertexToPoint(meshModel.vertices[polygon.pointIds[1]]));
            vertex3 = new Vector(Utils.VertexToPoint(meshModel.vertices[polygon.pointIds[2]]));
            normal2 = Vector.CrossProduct(vertex1 - vertex2, vertex2 - vertex3);

            var angle = (180 / Math.PI) * Vector.Angle(normal1, normal2);

            if (angle < 25)
            {
              meshModel.edges.Add(edge, EdgeStatus.HiddenEdge);
            }
            else
            {
              meshModel.edges.Add(edge, EdgeStatus.VisibleEdge);
            }
          }
        }
      }

      return meshModel;
    }

    public static MeshModel MeshToNative2(IEnumerable<Mesh> meshes)
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

        var renderMaterial = mesh["renderMaterial"] as RenderMaterial;
        if (renderMaterial != null)
        {
          Model.MeshModel.Material material = MaterialToNative(renderMaterial);
          polygons.ForEach(p => p.material = meshModel.materials.Count);
          meshModel.materials.Add(material);
        }
        meshModel.ids.Add(mesh.id);
      }

      return meshModel;
    }

    private static IEnumerable<double> FlattenPoint(MeshModel.Vertex vertex)
    {
      return new List<double> { vertex.x, vertex.y, vertex.z };
    }

    private static IEnumerable<int> PolygonToSpeckle(MeshModel.Polygon polygon, int offset = 0)
    {
      var vertexIds = new List<int> { polygon.pointIds.Count };
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

    private static Model.MeshModel.Material MaterialToNative(RenderMaterial renderMaterial)
    {
      Model.MeshModel.Material.Color ConvertColor(System.Drawing.Color color)
      {
        // In AC the Colors are encoded in ushort
        return new Model.MeshModel.Material.Color { red = color.R * 256, green = color.G * 256, blue = color.B * 256 };
      }

      return new Model.MeshModel.Material
      {
        name = renderMaterial.name,
        ambientColor = ConvertColor(System.Drawing.Color.FromArgb(renderMaterial.diffuse)),
        emissionColor = ConvertColor(System.Drawing.Color.FromArgb(renderMaterial.emissive)),
        transparency = (short)((1.0 - renderMaterial.opacity) * 100.0)
      };
    }
  }
}
