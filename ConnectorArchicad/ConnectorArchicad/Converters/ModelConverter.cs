using System;
using System.Collections.Generic;
using System.Linq;
using Archicad.Converters;
using Archicad.Model;
using Objects.Geometry;
using Objects.Other;
using Objects.Utils;
using Speckle.Core.Kits;
using static Archicad.Model.MeshModel;

namespace Archicad.Operations
{
  public static class ModelConverter
  {
    private static readonly double angleCosLimit = Math.Cos(Math.PI / 4);

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
      var context = Archicad.Helpers.Timer.Context.Peek;
      using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.MeshToNative))
      {
        var mergedVertexIndices = new Dictionary<Vertex, int>();
        var originalToMergedVertexIndices = new List<int>();
        var neigbourPolygonsByEdge = new Dictionary<Tuple<int, int>, List<int>>();
        var polygonNormals = new Dictionary<Polygon, System.Numerics.Vector3>();

        var vertexOffset = 0;

        var meshModel = new MeshModel();
        var enumerable = meshes as Mesh[] ?? meshes.ToArray();

        #region Local Funcitions
        // converts from original to merged vertex index
        int ToMergedVertexIndex(int i) => originalToMergedVertexIndices[i + vertexOffset];
        #endregion

        foreach (var mesh in enumerable)
        {
          MeshModel.Material material = null;
          if (mesh["renderMaterial"] is RenderMaterial renderMaterial)
          {
            material = MaterialToNative(renderMaterial);
            meshModel.materials.Add(material);
          }

          foreach (var vertex in mesh.GetPoints().Select(p => Utils.PointToNative(p)))
          {
            if (mergedVertexIndices.TryGetValue(vertex, out int idx))
            {
              originalToMergedVertexIndices.Add(idx);
            }
            else
            {
              originalToMergedVertexIndices.Add(mergedVertexIndices.Count);
              mergedVertexIndices.Add(vertex, mergedVertexIndices.Count);
              meshModel.vertices.Add(vertex);
            }
          }

          for (var i = 0; i < mesh.faces.Count; ++i)
          {
            var polygon = new Polygon();

            var n = mesh.faces[i];
            if (n < 3) n += 3;

            for (var vertexIdx = i+1; vertexIdx <= i+n; vertexIdx++)
            {
              var pointId = ToMergedVertexIndex(mesh.faces[vertexIdx]);
              if (polygon.pointIds.Count == 0 || pointId != polygon.pointIds[^1])
                polygon.pointIds.Add(pointId);
            }

            if (polygon.pointIds[0] == polygon.pointIds[^1])
            {
              polygon.pointIds.RemoveAt(0);
            }

            if (material != null)
            {
              polygon.material = meshModel.materials.Count - 1;
            }

            // check result polygon
            if (polygon.pointIds.Count >= 3)
            {
              if (meshModel.IsCoplanar(polygon))
              {
                ProcessPolygonEdges(meshModel, neigbourPolygonsByEdge, polygonNormals, polygon);
                meshModel.polygons.Add(polygon);
              }
              else
              {
                var triangleFaces = MeshTriangulationHelper.TriangulateFace(i, mesh, includeIndicators: false);
                for (int triangleStartIdx = 0; triangleStartIdx < triangleFaces.Count; triangleStartIdx += 3)
                {
                  var triangle = new Polygon { material = polygon.material };
                  for (int triangleVertexIdx = 0; triangleVertexIdx < 3; triangleVertexIdx++)
                  {
                    int trianglePointId = ToMergedVertexIndex(triangleFaces[triangleStartIdx + triangleVertexIdx]);
                    triangle.pointIds.Add(trianglePointId);
                  }

                  ProcessPolygonEdges(meshModel, neigbourPolygonsByEdge, polygonNormals, triangle);
                  meshModel.polygons.Add(triangle);
                }
              }
            }

            i += n;
          }
          vertexOffset += mesh.VerticesCount;

          meshModel.ids.Add(mesh.id);
        }

        return meshModel;
      }
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

        if (mesh["renderMaterial"] is RenderMaterial renderMaterial)
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

    private static void ProcessPolygonEdges(MeshModel meshModel, Dictionary<Tuple<int, int>, List<int>> neigbourPolygonsByEdge, Dictionary<Polygon, System.Numerics.Vector3> polygonNormals, Polygon polygon)
    {
      for (var pointIdx = 0; pointIdx < polygon.pointIds.Count; pointIdx++)
      {
        var edge = new Tuple<int, int>(polygon.pointIds[pointIdx], polygon.pointIds[(pointIdx + 1) % polygon.pointIds.Count]);
        if (TryGetNeigbourPolygonListByEdge(neigbourPolygonsByEdge, ref edge, out List<int> neigbourPolygonIdxs))
        {
          if (!neigbourPolygonIdxs.Contains(meshModel.polygons.Count))
          {
            neigbourPolygonIdxs.Add(meshModel.polygons.Count);

            if (neigbourPolygonIdxs.Count > 2)
              meshModel.edges[edge] = EdgeStatus.HiddenEdge;
            else if (IsHiddenEdge(edge, meshModel.polygons[neigbourPolygonIdxs[0]], polygon, polygonNormals, meshModel))
            {
              meshModel.edges[edge] = EdgeStatus.HiddenEdge;
            }
          }
        }
        else
        {
          neigbourPolygonsByEdge.Add(edge, new List<int> { meshModel.polygons.Count });
          meshModel.edges.Add(edge, EdgeStatus.VisibleEdge);
        }
      }
    }

    // try to find the list of neighbouring polygons of an edge
    // returns true if the edge or its inversion is present in neigbourPolygonsByEdge dictionary as key
    private static bool TryGetNeigbourPolygonListByEdge(Dictionary<Tuple<int, int>, List<int>> neigbourPolygonsByEdge, ref Tuple<int, int> edge, out List<int> neigbourPolygonIndices)
    {
      if (neigbourPolygonsByEdge.TryGetValue(edge, out neigbourPolygonIndices))
        return true;
      edge = new Tuple<int, int>(edge.Item2, edge.Item1);
      return neigbourPolygonsByEdge.TryGetValue(edge, out neigbourPolygonIndices);
    }

    private static System.Numerics.Vector3 GetOrientedNormal (Polygon polygon, Dictionary<Polygon, System.Numerics.Vector3> polygonNormals, MeshModel meshModel)
    {
      if (polygonNormals.TryGetValue(polygon, out System.Numerics.Vector3 normal))
        return normal;

      normal = new System.Numerics.Vector3 ();
      System.Numerics.Vector3 vertex0, vertex1, vertex2;

      vertex0 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[0]]);

      int count = polygon.pointIds.Count;
      for (int first = count - 1, second = 0; second < count; first = second++)
      { 
        vertex1 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[first]]);
        vertex2 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[second]]);

        normal += System.Numerics.Vector3.Cross (vertex1 - vertex0, vertex2 - vertex0);
      }

      polygonNormals.Add(polygon, normal);
      return normal;
    }

    private static int GetOrientation (Tuple<int, int> edge, Polygon polygon)
    {
      int count = polygon.pointIds.Count;
      for (int first = count - 1, second = 0; second < count; first = second++)
      {
        if (polygon.pointIds[first] == edge.Item1 && polygon.pointIds[second] == edge.Item2)
          return 1;
        if (polygon.pointIds[first] == edge.Item2 && polygon.pointIds[second] == edge.Item1)
          return -1;
      }
      return 0;
    }

    private static bool IsHiddenEdge(Tuple<int, int> edge, Polygon polygon1, Polygon polygon2, Dictionary<Polygon, System.Numerics.Vector3> polygonNormals, MeshModel meshModel)
    {
      System.Numerics.Vector3 normal1 = GetOrientation(edge, polygon1) * GetOrientedNormal(polygon1, polygonNormals, meshModel);
      System.Numerics.Vector3 normal2 = -1 * GetOrientation(edge, polygon2) * GetOrientedNormal(polygon2, polygonNormals, meshModel);

      normal1 = System.Numerics.Vector3.Normalize(normal1);
      normal2 = System.Numerics.Vector3.Normalize(normal2);

      var angleCos = System.Numerics.Vector3.Dot(normal1, normal2);

      return angleCos > angleCosLimit;
    }
  }
}
