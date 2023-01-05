using System.Collections.Generic;

namespace Archicad.Model
{
  public sealed class MeshModel
  {
    #region --- Classes ---

    public sealed class Vertex
    {
      #region --- Fields ---

      public double x { get; set; }

      public double y { get; set; }

      public double z { get; set; }

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

      public double transparency { get; set; }

      #endregion
    }

    public sealed class Polygon
    {
      public List<int> pointIds { get; set; } = new List<int>();

      public int material { get; set; }
    }

    #endregion

    #region --- Fields ---

    public List<Polygon> polygons { get; set; } = new List<Polygon>();

    public List<Vertex> vertices { get; set; } = new List<Vertex>();

    public List<Material> materials { get; set; } = new List<Material>();

    #endregion
  }
}
