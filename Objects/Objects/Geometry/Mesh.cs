using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Objects.Geometry
{
  public class Mesh : Base, IHasBoundingBox, IHasVolume, IHasArea
  {
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> vertices { get; set; } = new List<double>();
    
    [DetachProperty]
    [Chunkable(62500)]
    public List<int> faces { get; set; } = new List<int>();

    /// <summary> Vertex colors as ARGB <see cref="int"/>s</summary>
    [DetachProperty]
    [Chunkable(62500)]
    public List<int> colors { get; set; } = new List<int>();

    [DetachProperty]
    [Chunkable(31250)]
    public List<double> textureCoordinates { get; set; } = new List<double>();

    public Box bbox { get; set; }

    public double area { get; set; }

    public double volume { get; set; }

    public string units { get; set; }

    public Mesh()
    {

    }

    public Mesh(double[] vertices, int[] faces, int[] colors = null, double[] texture_coords = null, string units = Units.Meters, string applicationId = null)
    {
      this.vertices = vertices.ToList();
      this.faces = faces.ToList();
      this.colors = colors?.ToList();
      this.textureCoordinates = texture_coords?.ToList();
      this.applicationId = applicationId;
      this.units = units;
    }
    
    #region Convenience Methods
    
    public int VerticesCount => vertices.Count / 3;
    public int TextureCoordinatesCount => textureCoordinates.Count / 2;

    /// <summary>
    /// Gets a vertex as a <see cref="Point"/> by <paramref name="index"/>
    /// </summary>
    /// <param name="index">The index of the vertex</param>
    /// <returns>Vertex as a <see cref="Point"/></returns>
    public Point GetPointAtIndex(int index)
    {
      index *= 3;
      return new Point(
        vertices[index],
        vertices[index + 1], 
        vertices[index + 2],
        units,
        applicationId
        );
    }
    
    /// <summary>
    /// Gets a texture coordinate as a <see cref="ValueTuple{T1,T2}"/> by <paramref name="index"/>
    /// </summary>
    /// <param name="index">The index of the texture coordinate</param>
    /// <returns>Texture coordinate as a <see cref="ValueTuple{T1,T2}"/></returns>
    public (double,double) GetTextureCoordinateAtIndex(int index)
    {
      index *= 2;
      return (textureCoordinates[index], textureCoordinates[index + 1]);
    }

    /// <summary>
    /// If not already so, this method will align <see cref="Mesh.vertices"/>
    /// such that a vertex and its corresponding texture coordinates have the same index.
    /// This alignment is what is expected by most applications.<br/>
    /// </summary>
    /// <remarks>
    /// If the calling application expects 
    /// <code>vertices.count == textureCoordinates.count</code>
    /// Then this method should be called by the <c>MeshToNative</c> method before parsing <see cref="Mesh.vertices"/> and <see cref="Mesh.faces"/>
    /// to ensure compatibility with geometry originating from applications that map vertices to texture-coordinates using vertex instance index (rather than vertex index)
    /// <br/>
    /// <see cref="Mesh.vertices"/>, <see cref="Mesh.colors"/>, and <see cref="faces"/> lists will be modified to contain no shared vertices (vertices shared between polygons)
    /// </remarks>
    public void AlignVerticesWithTexCoordsByIndex()
    {
      if (textureCoordinates.Count == 0) return;
      if (TextureCoordinatesCount == VerticesCount) return; //Tex-coords already aligned as expected
      
      var facesUnique = new List<int>(faces.Count);
      var verticesUnique = new List<double>(TextureCoordinatesCount * 3);
      bool hasColors = colors.Count > 0;
      var colorsUnique = hasColors? new List<int>(TextureCoordinatesCount) : null;
      
      
      int nIndex = 0;
      while (nIndex < faces.Count)
      {
        int n = faces[nIndex];
        if (n < 3) n += 3; // 0 -> 3, 1 -> 4
        
        if (nIndex + n >= faces.Count) break; //Malformed face list
        
        facesUnique.Add(n);
        for (int i = 1; i <= n; i++)
        {
          int vertIndex = faces[nIndex + i];
          int newVertIndex = verticesUnique.Count / 3;
          
          var (x, y, z) = GetPointAtIndex(vertIndex);
          verticesUnique.Add(x);
          verticesUnique.Add(y);
          verticesUnique.Add(z);

          colorsUnique?.Add(colors[vertIndex]);
          facesUnique.Add(newVertIndex);
        }
        
        nIndex += n + 1;
      }
      
      vertices = verticesUnique;
      colors = colorsUnique ?? colors;
      faces = facesUnique;
    }
    
    #endregion
  }
}
