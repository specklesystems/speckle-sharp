using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace Archicad.Model;

public sealed class MeshModel
{
  public enum EdgeStatus
  {
    HiddenEdge = 1, // invisible
    SmoothEdge = 2, // visible if countour bit
    VisibleEdge = 3 // visible (AKA hard, sharp, welded edge)
  };

  #region --- Classes ---

  public class MeshModelEdgeConverter : JsonConverter<Dictionary<EdgeId, EdgeData>>
  {
    public override void WriteJson(JsonWriter writer, Dictionary<EdgeId, EdgeData> value, JsonSerializer serializer)
    {
      StringBuilder jsonString = new();
      jsonString.Append("[");

      bool first = true;
      foreach (var entry in value)
      {
        // skip hidden edges
        if (entry.Value.edgeStatus == EdgeStatus.HiddenEdge)
        {
          continue;
        }

        if (!first)
        {
          jsonString.Append(", ");
        }
        else
        {
          first = false;
        }

        jsonString.Append("{ \"v1\": ");
        jsonString.Append(entry.Key.vertexId1.ToString());

        jsonString.Append(", \"v2\": ");
        jsonString.Append(entry.Key.vertexId2.ToString());

        if (entry.Value.polygonId1 != EdgeData.InvalidPolygonId)
        {
          jsonString.Append(", \"p1\": ");
          jsonString.Append(entry.Value.polygonId1.ToString());
        }

        if (entry.Value.polygonId2 != EdgeData.InvalidPolygonId)
        {
          jsonString.Append(", \"p2\": ");
          jsonString.Append(entry.Value.polygonId2.ToString());
        }

        jsonString.Append(", \"s\": \"");
        jsonString.Append(entry.Value.edgeStatus.ToString());

        jsonString.Append("\" }");
      }

      jsonString.Append("]");
      writer.WriteRawValue(jsonString.ToString());
    }

    public override Dictionary<EdgeId, EdgeData> ReadJson(
      JsonReader reader,
      Type objectType,
      Dictionary<EdgeId, EdgeData> existingValue,
      bool hasExistingValue,
      JsonSerializer serializer
    )
    {
      Dictionary<EdgeId, EdgeData> edges = new();

      JArray ja = JArray.Load(reader);

      foreach (JObject jo in ja)
      {
        JToken v1;
        jo.TryGetValue("v1", out v1);

        JToken v2;
        jo.TryGetValue("v2", out v2);

        JToken s;
        jo.TryGetValue("s", out s);

        JToken p1;
        jo.TryGetValue("p1", out p1);

        JToken p2;
        jo.TryGetValue("p2", out p2);

        if (v1 == null || v2 == null || s == null)
        {
          continue;
        }

        EdgeId edgeId = new(((int)v1), ((int)v2));

        MeshModel.EdgeStatus edgeStatus = MeshModel.EdgeStatus.HiddenEdge;
        if (((string)s).Equals("SmoothEdge"))
        {
          edgeStatus = MeshModel.EdgeStatus.SmoothEdge;
        }
        else if (((string)s).Equals("VisibleEdge"))
        {
          edgeStatus = MeshModel.EdgeStatus.VisibleEdge;
        }

        EdgeData edgeData =
          new(
            edgeStatus,
            p1 != null ? ((int)p1) : MeshModel.EdgeData.InvalidPolygonId,
            p2 != null ? ((int)p2) : MeshModel.EdgeData.InvalidPolygonId
          );

        edges.Add(edgeId, edgeData);
      }

      return edges;
    }
  }

  public sealed class Vertex
  {
    #region --- Fields ---

    public double x { get; set; }

    public double y { get; set; }

    public double z { get; set; }

    public bool Equals(Vertex vertex) => vertex.x.Equals(x) && vertex.y.Equals(y) && vertex.z.Equals(z);

    public override bool Equals(object o) => Equals(o as Vertex);

    public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode();

    #endregion
  }

  public sealed class EdgeId
  {
    #region --- Fields ---

    public int vertexId1 { get; set; }

    public int vertexId2 { get; set; }

    #endregion

    #region --- Methods ---

    public EdgeId(int vertexId1, int vertexId2)
    {
      this.vertexId1 = vertexId1;
      this.vertexId2 = vertexId2;
    }

    public bool Equals(EdgeId edge) => edge.vertexId1.Equals(vertexId1) && edge.vertexId2.Equals(vertexId2);

    public override bool Equals(object o) => Equals(o as EdgeId);

    public override int GetHashCode() => vertexId1.GetHashCode() ^ vertexId2.GetHashCode();

    #endregion
  }

  public sealed class EdgeData
  {
    public const int InvalidPolygonId = -1;

    #region --- Fields ---

    public EdgeStatus edgeStatus { get; set; }

    public int polygonId1 { get; set; }

    public int polygonId2 { get; set; }

    #endregion

    #region --- Methods ---

    public EdgeData(EdgeStatus edgeStatus, int polygonId1 = InvalidPolygonId, int polygonId2 = InvalidPolygonId)
    {
      this.edgeStatus = edgeStatus;
      this.polygonId1 = polygonId1;
      this.polygonId2 = polygonId2;
    }

    #endregion
  }

  public sealed class Material
  {
    #region --- Classes ---

    public class Color
    {
      public int red { get; set; }

      public int green { get; set; }

      public int blue { get; set; }
    }

    #endregion

    #region --- Fields ---

    public string name { get; set; }

    public Color ambientColor { get; set; }

    public Color emissionColor { get; set; }

    public short transparency { get; set; }

    #endregion
  }

  public sealed class Polygon
  {
    public List<int> pointIds { get; set; } = new List<int>();

    public int material { get; set; }
  }

  #endregion

  #region --- Fields ---

  public List<string> ids { get; set; } = new List<string>();

  public List<Vertex> vertices { get; set; } = new List<Vertex>();

  [JsonConverter(typeof(MeshModelEdgeConverter))]
  public Dictionary<EdgeId, EdgeData> edges { get; set; } = new Dictionary<EdgeId, EdgeData>();

  public List<Polygon> polygons { get; set; } = new List<Polygon>();

  public List<Material> materials { get; set; } = new List<Material>();

  public bool IsCoplanar(Polygon polygon)
  {
    Vector vector1 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[0]]);
    Vector vector2 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[1]]);
    Vector vector3 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[2]]);
    for (int i = 3; i < polygon.pointIds.Count; i++)
    {
      Vector vector4 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[i]]);
      if (!IsCoplanar(vector1, vector2, vector3, vector4))
      {
        return false;
      }
    }
    return true;
  }

  private bool IsCoplanar(Vector vector1, Vector vector2, Vector vector3, Vector vector4)
  {
    var dotProduct = Vector.DotProduct(vector2 - vector1, Vector.CrossProduct(vector4 - vector1, vector3 - vector1));
    return Math.Abs(dotProduct) < Speckle.Core.Helpers.Constants.SmallEps;
  }

  #endregion
}
