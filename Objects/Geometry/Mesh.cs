using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Speckle.Objects.Geometry
{
  public class Mesh : Base, IGeometry
  {
    public List<double> vertices { get; set; } = new List<double>();

    /// <summary>The faces array.</summary>
    public List<int> faces { get; set; } = new List<int>();

    /// <summary>If any, the colours per vertex.</summary>
    public List<int> colors { get; set; } = new List<int>();

    /// <summary>If any, the colours per vertex.</summary>
    public List<double> textureCoordinates { get; set; } = new List<double>();

    public Mesh()
    {

    }

    public Mesh(double[] vertices, int[] faces, int[] colors, double[] texture_coords, string applicationId = null)
    {
      this.vertices = vertices.ToList();
      this.faces = faces.ToList();
      this.colors = colors.ToList();
      this.applicationId = applicationId;
    }
  }
}
