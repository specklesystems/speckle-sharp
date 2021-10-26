using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Geometry
{
  public class Mesh : Base, IHasBoundingBox, IHasVolume, IHasArea
  {
    [DetachProperty]
    [Chunkable(31250)]
    public List<double> vertices { get; set; } = new List<double>();


    public void DuplicateSharedVertices()
    {
      var facesUnique = new List<int>(faces.Count);
      var verticesUnique = new List<double>(vertices.Count); //will be resized larger
      
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
          int xIndex = vertIndex * 3;
          int newVertIndex = verticesUnique.Count / 3;
          
          verticesUnique.Add(vertices[xIndex]); //x
          verticesUnique.Add(vertices[xIndex + 1]); //y
          verticesUnique.Add(vertices[xIndex + 2]); //z

          facesUnique.Add(newVertIndex);
        }
        
        nIndex += n + 1;
      }
      
      vertices = verticesUnique;
      faces = facesUnique;
    }
    
    
    
    
    // public void DuplicateSharedVertices()
    // {
    //   var verticesUnique = new List<double>(); //vertices.Count); 
    //   var facesUnique = new List<int>(); //faces.Count);
    //   
    //   int nIndex = 0;
    //   while (nIndex < faces.Count)
    //   {
    //     int n = faces[nIndex];
    //     if (n < 3) n += 3; // 0 -> 3, 1 -> 4
    //
    //     if (nIndex + n >= faces.Count) break; //Malformed face list
    //     
    //     facesUnique.Add(n);
    //     for (int i = 1; i <= n; i++)
    //     {
    //       int xIndex = faces[nIndex + i] * 3;
    //       int vertIndex = verticesUnique.Count / 3;
    //         
    //       verticesUnique.Add(vertices[xIndex]);
    //       verticesUnique.Add(vertices[xIndex + 1]);
    //       verticesUnique.Add(vertices[xIndex + 2]);
    //       
    //       facesUnique.Add(vertIndex);
    //     }
    //
    //     nIndex += n + 1;
    //   }
    //
    //   vertices = verticesUnique;
    //   faces = facesUnique;
    // }

    [DetachProperty]
    [Chunkable(62500)]
    public List<int> faces { get; set; } = new List<int>();

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
  }
}
