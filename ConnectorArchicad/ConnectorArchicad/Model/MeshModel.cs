using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Primitives;
using Speckle.Newtonsoft.Json;
using Objects.Geometry;

namespace Archicad.Model
{
  public sealed class MeshModel
  {
    public enum EdgeStatus 
    {
      HiddenEdge = 1, // invisible
      SmoothEdge = 2, // visible if countour bit
      VisibleEdge = 3 // visible (AKA hard, sharp, welded edge)
    };

    #region --- Classes ---

    public class MeshModelEdgeConverter : JsonConverter<Dictionary<Tuple<int, int>, EdgeStatus>>
    {
      public override void WriteJson(JsonWriter writer, Dictionary<Tuple<int, int>, EdgeStatus> value, JsonSerializer serializer)
      {
        StringBuilder jsonString = new StringBuilder();
        jsonString.Append("[");

        bool first = true;
        foreach (var entry in value)
        {
          if (!first)
            jsonString.Append(", ");
          else
            first = false;

          jsonString.Append("{ \"first\": ");

          jsonString.Append("{ \"first\": ");
          jsonString.Append(entry.Key.Item1.ToString());
          jsonString.Append(", \"second\": ");
          jsonString.Append(entry.Key.Item2.ToString());
          jsonString.Append(" }");

          jsonString.Append(", \"second\" :");
          jsonString.Append(((byte)entry.Value).ToString());
          jsonString.Append(" }");
        }
        jsonString.Append ("]");
        writer.WriteRawValue(jsonString.ToString());
      }

      public override Dictionary<Tuple<int, int>, EdgeStatus> ReadJson(JsonReader reader, Type objectType, Dictionary<Tuple<int, int>, EdgeStatus> existingValue, bool hasExistingValue, JsonSerializer serializer)
      {
        return new Dictionary<Tuple<int, int>, EdgeStatus>();
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

    public List<Polygon> polygons { get; set; } = new List<Polygon>();

    public List<Vertex> vertices { get; set; } = new List<Vertex>();

    public List<Material> materials { get; set; } = new List<Material>();

    [JsonConverter(typeof(MeshModelEdgeConverter))]
    public Dictionary<Tuple<int, int>, EdgeStatus> edges { get; set; } = new Dictionary<Tuple<int, int>, EdgeStatus>();

    public bool IsCoplanar (Polygon polygon)
    {
      Vector vector1 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[0]]);
      Vector vector2 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[1]]);
      Vector vector3 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[2]]);
      for (int i = 3; i < polygon.pointIds.Count; i++)
      {
        Vector vector4 = Archicad.Converters.Utils.VertexToVector(vertices[polygon.pointIds[i]]);
        if (!IsCoplanar(vector1, vector2, vector3, vector4))
          return false;
      }
      return true;
    }

    private bool IsCoplanar(Vector vector1, Vector vector2, Vector vector3, Vector vector4)
    {
      var dotProduct = Vector.DotProduct(vector2 - vector1, Vector.CrossProduct(vector4 - vector1, vector3 - vector1)); 
      return Math.Abs(dotProduct) < Speckle.Core.Helpers.Constants.Eps;
    }

    #endregion
  }
}
