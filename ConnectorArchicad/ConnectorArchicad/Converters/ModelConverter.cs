using System;
using System.Collections.Generic;
using System.Linq;
using Archicad.Converters;
using Archicad.Model;
using Objects;
using Objects.BuiltElements.Archicad;
using Objects.Geometry;
using Objects.Other;
using Objects.Utils;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using static Archicad.Model.MeshModel;

namespace Archicad.Operations;

public static class ModelConverter
{
  private static readonly double angleCosLimit = Math.Cos(Math.PI / 4);

  public static List<Mesh> MeshesToSpeckle(MeshModel meshModel)
  {
    var context = Archicad.Helpers.Timer.Context.Peek;
    using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.MeshToSpeckle))
    {
      var materials = meshModel.materials.Select(MaterialToSpeckle).ToList();
      var meshes = materials.Select(m => new Mesh { units = Units.Meters, ["renderMaterial"] = m }).ToList();
      var vertCount = new int[materials.Count];

      foreach (var poly in meshModel.polygons)
      {
        var meshIndex = poly.material;
        meshes[meshIndex]
          .vertices.AddRange(poly.pointIds.SelectMany(id => FlattenPoint(meshModel.vertices[id])).ToList());
        meshes[meshIndex].faces.AddRange(PolygonToSpeckle(poly, vertCount[meshIndex]));
        vertCount[meshIndex] += poly.pointIds.Count;
      }

      return meshes;
    }
  }

  public static List<Speckle.Core.Models.Base> MeshesAndLinesToSpeckle(MeshModel meshModel)
  {
    List<Speckle.Core.Models.Base> meshes = MeshesToSpeckle(meshModel).Cast<Speckle.Core.Models.Base>().ToList();

    List<Line> lines = new();
    foreach (var edge in meshModel.edges)
    {
      if (edge.Value.polygonId1 == EdgeData.InvalidPolygonId && edge.Value.polygonId2 == EdgeData.InvalidPolygonId)
      {
        var line = new Line(
          new Point(
            meshModel.vertices[edge.Key.vertexId1].x,
            meshModel.vertices[edge.Key.vertexId1].y,
            meshModel.vertices[edge.Key.vertexId1].z
          ),
          new Point(
            meshModel.vertices[edge.Key.vertexId2].x,
            meshModel.vertices[edge.Key.vertexId2].y,
            meshModel.vertices[edge.Key.vertexId2].z
          )
        );

        lines.Add(line);
      }
    }

    meshes.AddRange(lines.Cast<Speckle.Core.Models.Base>().ToList());
    return meshes;
  }

  public static MeshModel MeshToNative(IEnumerable<Mesh> meshes)
  {
    var context = Archicad.Helpers.Timer.Context.Peek;
    using (context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.MeshToNative))
    {
      var mergedVertexIndices = new Dictionary<Vertex, int>();
      var originalToMergedVertexIndices = new List<int>();
      var neigbourPolygonsByEdge = new Dictionary<EdgeId, List<int>>();
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
          if (n < 3)
          {
            n += 3;
          }

          for (var vertexIdx = i + 1; vertexIdx <= i + n; vertexIdx++)
          {
            var pointId = ToMergedVertexIndex(mesh.faces[vertexIdx]);
            if (polygon.pointIds.Count == 0 || pointId != polygon.pointIds[^1])
            {
              polygon.pointIds.Add(pointId);
            }
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
      if (n < 3)
      {
        n += 3;
      }

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
      return new Model.MeshModel.Material.Color
      {
        red = color.R * 256,
        green = color.G * 256,
        blue = color.B * 256
      };
    }

    return new Model.MeshModel.Material
    {
      name = renderMaterial.name,
      ambientColor = ConvertColor(System.Drawing.Color.FromArgb(renderMaterial.diffuse)),
      emissionColor = ConvertColor(System.Drawing.Color.FromArgb(renderMaterial.emissive)),
      transparency = (short)((1.0 - renderMaterial.opacity) * 100.0)
    };
  }

  private static void ProcessPolygonEdges(
    MeshModel meshModel,
    Dictionary<EdgeId, List<int>> neigbourPolygonsByEdge,
    Dictionary<Polygon, System.Numerics.Vector3> polygonNormals,
    Polygon polygon
  )
  {
    for (var pointIdx = 0; pointIdx < polygon.pointIds.Count; pointIdx++)
    {
      var edge = new EdgeId(polygon.pointIds[pointIdx], polygon.pointIds[(pointIdx + 1) % polygon.pointIds.Count]);
      if (TryGetNeigbourPolygonListByEdge(neigbourPolygonsByEdge, ref edge, out List<int> neigbourPolygonIdxs))
      {
        if (!neigbourPolygonIdxs.Contains(meshModel.polygons.Count))
        {
          neigbourPolygonIdxs.Add(meshModel.polygons.Count);

          if (neigbourPolygonIdxs.Count > 2)
          {
            meshModel.edges[edge] = new EdgeData(EdgeStatus.HiddenEdge);
          }
          else if (IsHiddenEdge(edge, meshModel.polygons[neigbourPolygonIdxs[0]], polygon, polygonNormals, meshModel))
          {
            meshModel.edges[edge] = new EdgeData(EdgeStatus.HiddenEdge);
          }
        }
      }
      else
      {
        neigbourPolygonsByEdge.Add(edge, new List<int> { meshModel.polygons.Count });
        meshModel.edges.Add(edge, new EdgeData(EdgeStatus.VisibleEdge));
      }
    }
  }

  // try to find the list of neighbouring polygons of an edge
  // returns true if the edge or its inversion is present in neigbourPolygonsByEdge dictionary as key
  private static bool TryGetNeigbourPolygonListByEdge(
    Dictionary<EdgeId, List<int>> neigbourPolygonsByEdge,
    ref EdgeId edge,
    out List<int> neigbourPolygonIndices
  )
  {
    if (neigbourPolygonsByEdge.TryGetValue(edge, out neigbourPolygonIndices))
    {
      return true;
    }

    edge = new EdgeId(edge.vertexId2, edge.vertexId1);
    return neigbourPolygonsByEdge.TryGetValue(edge, out neigbourPolygonIndices);
  }

  private static System.Numerics.Vector3 GetOrientedNormal(
    Polygon polygon,
    Dictionary<Polygon, System.Numerics.Vector3> polygonNormals,
    MeshModel meshModel
  )
  {
    if (polygonNormals.TryGetValue(polygon, out System.Numerics.Vector3 normal))
    {
      return normal;
    }

    normal = new System.Numerics.Vector3();
    System.Numerics.Vector3 vertex0,
      vertex1,
      vertex2;

    vertex0 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[0]]);

    int count = polygon.pointIds.Count;
    for (int first = count - 1, second = 0; second < count; first = second++)
    {
      vertex1 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[first]]);
      vertex2 = Utils.VertexToVector3(meshModel.vertices[polygon.pointIds[second]]);

      normal += System.Numerics.Vector3.Cross(vertex1 - vertex0, vertex2 - vertex0);
    }

    polygonNormals.Add(polygon, normal);
    return normal;
  }

  private static int GetOrientation(EdgeId edge, Polygon polygon)
  {
    int count = polygon.pointIds.Count;
    for (int first = count - 1, second = 0; second < count; first = second++)
    {
      if (polygon.pointIds[first] == edge.vertexId1 && polygon.pointIds[second] == edge.vertexId2)
      {
        return 1;
      }

      if (polygon.pointIds[first] == edge.vertexId2 && polygon.pointIds[second] == edge.vertexId1)
      {
        return -1;
      }
    }
    return 0;
  }

  private static bool IsHiddenEdge(
    EdgeId edge,
    Polygon polygon1,
    Polygon polygon2,
    Dictionary<Polygon, System.Numerics.Vector3> polygonNormals,
    MeshModel meshModel
  )
  {
    System.Numerics.Vector3 normal1 =
      GetOrientation(edge, polygon1) * GetOrientedNormal(polygon1, polygonNormals, meshModel);
    System.Numerics.Vector3 normal2 =
      -1 * GetOrientation(edge, polygon2) * GetOrientedNormal(polygon2, polygonNormals, meshModel);

    normal1 = System.Numerics.Vector3.Normalize(normal1);
    normal2 = System.Numerics.Vector3.Normalize(normal2);

    var angleCos = System.Numerics.Vector3.Dot(normal1, normal2);

    return angleCos > angleCosLimit;
  }

  public static ICurve CreateOpeningOutline(ArchicadOpening opening)
  {
    double halfWidth = opening.width / 2.0 ?? 0.0;
    double halfHeight = opening.height / 2.0 ?? 0.0;
    Vector basePoint = new(0, 0, 0);
    Vector extrusionBasePoint = new(opening.extrusionGeometryBasePoint);

    // Speckle datastructure does not handle the translation component, which we will use manually later, so its left empty.
    System.DoubleNumerics.Matrix4x4 rotMatrix =
      new(
        (float)opening.extrusionGeometryXAxis.x,
        (float)opening.extrusionGeometryXAxis.y,
        (float)opening.extrusionGeometryXAxis.z,
        0,
        (float)opening.extrusionGeometryYAxis.x,
        (float)opening.extrusionGeometryYAxis.y,
        (float)opening.extrusionGeometryYAxis.z,
        0,
        (float)opening.extrusionGeometryZAxis.x,
        (float)opening.extrusionGeometryZAxis.y,
        (float)opening.extrusionGeometryZAxis.z,
        0,
        0,
        0,
        0,
        1
      );

    Objects.Other.Transform transform = new(System.DoubleNumerics.Matrix4x4.Transpose(rotMatrix));

    AdjustBasePoint(ref basePoint, halfWidth, halfHeight, opening.anchorIndex ?? 0);

    return opening.basePolygonType == "Rectangular"
      ? CreateRectangle(basePoint, transform, extrusionBasePoint, halfWidth, halfHeight)
      : CreateEllipse(basePoint, transform, extrusionBasePoint, halfWidth, halfHeight, opening);
  }

  private static readonly Action<Vector, double, double>[] anchorActions = new Action<Vector, double, double>[]
  {
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.x = halfWidth;
      basePoint.y = -halfHeight;
    }, // APIAnc_LT
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.y = -halfHeight;
    }, // APIAnc_MT
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.x = -halfWidth;
      basePoint.y = -halfHeight;
    }, // APIAnc_RT
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.x = halfWidth;
    }, // APIAnc_LM
    (Vector basePoint, double halfWidth, double halfHeight) => { }, // APIAnc_MM
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.x = -halfWidth;
    }, // APIAnc_RM
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.x = halfWidth;
      basePoint.y = halfHeight;
    }, // APIAnc_LB
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.y = halfHeight;
    }, // APIAnc_MB
    (Vector basePoint, double halfWidth, double halfHeight) =>
    {
      basePoint.x = -halfWidth;
      basePoint.y = halfHeight;
    } // APIAnc_RB
  };

  private static void AdjustBasePoint(ref Vector basePoint, double halfWidth, double halfHeight, int anchor)
  {
    if (anchor >= 0 && anchor < anchorActions.Length)
    {
      anchorActions[anchor](basePoint, halfWidth, halfHeight);
    }
  }

  private static Polyline CreateRectangle(
    Vector basePoint,
    Objects.Other.Transform transform,
    Vector extrusionBasePoint,
    double halfWidth,
    double halfHeight
  )
  {
    var poly = new Objects.Geometry.Polyline
    {
      value = new List<double>(),
      closed = true,
      units = Units.Meters
    };

    // Coordinates of the four corners of the rectangle
    Vector[] points =
    {
      new(-halfWidth, -halfHeight, 0),
      new(halfWidth, -halfHeight, 0),
      new(halfWidth, halfHeight, 0),
      new(-halfWidth, halfHeight, 0)
    };

    // Transform the points to the correct position
    foreach (var point in points)
    {
      Vector transformedPoint = point + basePoint;
      transformedPoint.TransformTo(transform, out transformedPoint);
      transformedPoint += extrusionBasePoint;
      poly.value.AddRange(transformedPoint.ToList());
    }

    // Close the polyline
    poly.value.AddRange(poly.value.Take(3));

    return poly;
  }

  private static Ellipse CreateEllipse(
    Vector basePoint,
    Objects.Other.Transform transform,
    Vector extrusionBasePoint,
    double halfWidth,
    double halfHeight,
    ArchicadOpening opening
  )
  {
    Vector centerPoint = new(basePoint.x, basePoint.y, basePoint.z);
    centerPoint.TransformTo(transform, out centerPoint);
    centerPoint += extrusionBasePoint;

    Point center = new(centerPoint.x, centerPoint.y, centerPoint.z);

    Objects.Geometry.Plane plane =
      new(center, opening.extrusionGeometryZAxis, opening.extrusionGeometryXAxis, opening.extrusionGeometryYAxis);

    return new Ellipse(plane, halfWidth, halfHeight, Units.Meters);
  }

  public static void GetExtrusionParametersFromOutline(
    ICurve outline,
    out Vector extrusionBasePoint,
    out Vector extrusionXAxis,
    out Vector extrusionYAxis,
    out Vector extrusionZAxis,
    out double width,
    out double height
  )
  {
    // Assign default values to out parameters
    extrusionBasePoint = new Vector();
    extrusionXAxis = new Vector();
    extrusionYAxis = new Vector();
    extrusionZAxis = new Vector();
    width = 0;
    height = 0;

    if (outline is not Polyline polyline)
    {
      throw new SpeckleException(
        $"An opening of type {outline.GetType()} has been found - only Polyline openings are currently supported"
      );
    }

    // Form the 4 points of the rectangle from the polyline
    List<Vector> points = Enumerable
      .Range(0, polyline.value.Count / 3)
      .Select(i => new Vector(
        polyline.value[i * 3],
        polyline.value[i * 3 + 1],
        polyline.value[i * 3 + 2],
        polyline.units
      ))
      .ToList();

    Vector bottomLeft = Utils.ScaleToNative(points[0]);
    Vector topLeft = Utils.ScaleToNative(points[1]);
    Vector topRight = Utils.ScaleToNative(points[2]);
    Vector bottomRight = Utils.ScaleToNative(points[3]);

    // We set the anchor point to Middle-Middle, so we can calculate the extrusion base point more easily like so.
    extrusionBasePoint = (bottomLeft + bottomRight + topRight + topLeft) * 0.25;

    Vector verticalDiff = topRight - bottomRight;
    height = verticalDiff.Length;

    extrusionYAxis = verticalDiff / height;

    Vector horizontalDiff = bottomRight - bottomLeft;
    width = horizontalDiff.Length;

    // Calculate the extrusion X axis
    extrusionXAxis = horizontalDiff / width;

    // The last extrusion axis will be the cross product of the other two
    extrusionZAxis = Vector.CrossProduct(extrusionXAxis, extrusionYAxis);

    extrusionZAxis.Normalize();
  }
}
