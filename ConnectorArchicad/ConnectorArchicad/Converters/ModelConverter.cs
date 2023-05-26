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
      var mergedVertexIndices = new Dictionary<Vertex, int>();
      var originalToMergedVertexIndices = new List<int>();
      var neigbourPolygonsByEdge = new Dictionary<Tuple<int, int>, List<int>>();

      var vertexOffset = 0;

      var meshModel = new MeshModel();
      var enumerable = meshes as Mesh[] ?? meshes.ToArray();

      #region Local Funcitions
      // converts from original to merged vertex index
      int ToMergedVertexIndex(int i) => originalToMergedVertexIndices[i + vertexOffset];

      // try to find the list of neighbouring polygons of an edge
      // returns true if the edge or its inversion is present in neigbourPolygonsByEdge dictionary as key
      bool TryGetNeigPolygonListByEdge(ref Tuple<int, int> edge, out List<int> neigbourPolygonIdxs)
      {
        if (neigbourPolygonsByEdge.TryGetValue(edge, out neigbourPolygonIdxs))
          return true;
        edge = new Tuple<int, int>(edge.Item2, edge.Item1);
        return neigbourPolygonsByEdge.TryGetValue(edge, out neigbourPolygonIdxs);
      }
      #endregion

      foreach (var mesh in enumerable)
      {
        var renderMaterial = mesh["renderMaterial"] as RenderMaterial;
        MeshModel.Material material = null;
        if (renderMaterial != null)
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
          var neigPolygonsByEdgesToCheckIfSmooth = new List<Tuple<Tuple<int, int>, Polygon>>();

          var n = mesh.faces[i];
          if (n < 3) n += 3;

          int firstVertexIdx = i + 1;
          int lastVertexIdx = i + n;

          int startVertexIdx = firstVertexIdx;
          int endVertexIdx = startVertexIdx;

          bool stop = false;
          while (!stop)
          {
            // calculate startVertexIdx - skip similar vertices for startVertexIdx
            {
              var mergedVertexIndexToSkip = ToMergedVertexIndex(mesh.faces[startVertexIdx]);

              while (true)
              {
                bool looped = false;

                // get the next index
                var nextVertexIndex = startVertexIdx + 1;
                if (nextVertexIndex > lastVertexIdx)
                {
                  nextVertexIndex = firstVertexIdx;
                  looped = true;
                }

                if (mergedVertexIndexToSkip == ToMergedVertexIndex(mesh.faces[nextVertexIndex]))
                {
                  if (looped)
                  {
                    stop = true;
                    break;
                  }

                  startVertexIdx = nextVertexIndex;
                }
                else {
                  break;
                }
              }

              if (stop)
                break;
            }

            // calculate endVertexIdx
            if (startVertexIdx == lastVertexIdx)
            {
              endVertexIdx = firstVertexIdx;
              stop = true;
            }
            else
              endVertexIdx = startVertexIdx + 1;

            // get merged indices
            int startMergedVertexIdx = ToMergedVertexIndex(mesh.faces[startVertexIdx]);
            int endMergedVertexIdx = ToMergedVertexIndex(mesh.faces[endVertexIdx]);
            polygon.pointIds.Add(startMergedVertexIdx);

            // process the edge
            var edge = new Tuple<int, int>(startMergedVertexIdx, endMergedVertexIdx);
            if (TryGetNeigPolygonListByEdge(ref edge, out List<int> neigbourPolygonIdxs))
            {
              if (!neigbourPolygonIdxs.Contains(meshModel.polygons.Count))
              {
                neigbourPolygonIdxs.Add(meshModel.polygons.Count);

                if (neigbourPolygonIdxs.Count > 2)
                  meshModel.edges[edge] = EdgeStatus.HiddenEdge;
                else
                  neigPolygonsByEdgesToCheckIfSmooth.Add(new Tuple<Tuple<int, int>, Polygon>(edge, meshModel.polygons[neigbourPolygonIdxs[0]]));
              }
            }
            else
            {
              neigbourPolygonsByEdge.Add(edge, new List<int> { meshModel.polygons.Count });
              meshModel.edges.Add(edge, EdgeStatus.VisibleEdge);
            }

            startVertexIdx = endVertexIdx;
          }

          foreach (var neigPolygonByEdge in neigPolygonsByEdgesToCheckIfSmooth)
          {
            if (IsHiddenEdge(neigPolygonByEdge.Item1, neigPolygonByEdge.Item2, polygon, meshModel))
            {
              meshModel.edges[neigPolygonByEdge.Item1] = EdgeStatus.HiddenEdge;
            }
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
                  int edgeStartIdx = ToMergedVertexIndex(triangleFaces[triangleStartIdx + triangleVertexIdx]);
                  int edgeEndIdx = ToMergedVertexIndex(triangleFaces[triangleStartIdx + ((triangleVertexIdx + 1) % 3)]);
                  var edge = new Tuple<int, int>(edgeStartIdx, edgeEndIdx);

                  if (!TryGetNeigPolygonListByEdge(ref edge, out List<int> neigPolygonIdxs))
                  {
                    neigbourPolygonsByEdge.Add(edge, new List<int> { meshModel.polygons.Count });
                    meshModel.edges.Add(edge, EdgeStatus.HiddenEdge);
                  }
                  triangle.pointIds.Add(edgeStartIdx);
                }
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


    private static System.Numerics.Vector3 GetOrientedNormal (Tuple<int, int> edge, Polygon polygon, MeshModel meshModel)
    {
      System.Numerics.Vector3 normal = new System.Numerics.Vector3 ();
      System.Numerics.Vector3 vertex0, vertex1, vertex2;

      vertex0 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[0]]);

      int orientation = 0;
      int count = polygon.pointIds.Count;
      for (int first = count - 1, second = 0; second < count; first = second++)
      { 
        vertex1 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[first]]);
        vertex2 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[second]]);

        normal += System.Numerics.Vector3.Cross (vertex1 - vertex0, vertex2 - vertex0);

        if (polygon.pointIds[first] == edge.Item1 && polygon.pointIds[second] == edge.Item2)
          orientation = 1;
        if (polygon.pointIds[first] == edge.Item2 && polygon.pointIds[second] == edge.Item1)
          orientation = -1;
      }

      return normal * orientation;
    }

    private static bool IsHiddenEdge(Tuple<int, int> edge, Polygon polygon1, Polygon polygon2, MeshModel meshModel)
    {
      System.Numerics.Vector3 normal1 = GetOrientedNormal(edge, polygon1, meshModel);
      System.Numerics.Vector3 normal2 = -GetOrientedNormal(edge, polygon2, meshModel);

      normal1 = System.Numerics.Vector3.Normalize(normal1);
      normal2 = System.Numerics.Vector3.Normalize(normal2);

      var angleCos = System.Numerics.Vector3.Dot(normal1, normal2);

      return angleCos > angleCosLimit;
    }
  }
}
