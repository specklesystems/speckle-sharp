using System;
using System.Collections.Generic;
using System.Linq;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry;

public class Mesh : Base, IHasBoundingBox, IHasVolume, IHasArea, ITransformable<Mesh>
{
  public Mesh() { }

  /// <summary>
  /// Constructs a new mesh from it's raw values.
  /// </summary>
  /// <param name="vertices"></param>
  /// <param name="faces"></param>
  /// <param name="colors"></param>
  /// <param name="texture_coords"></param>
  /// <param name="units"></param>
  /// <param name="applicationId"></param>
  public Mesh(
    List<double> vertices,
    List<int> faces,
    List<int>? colors = null,
    List<double>? texture_coords = null,
    string units = Units.Meters,
    string? applicationId = null
  )
  {
    this.vertices = vertices;
    this.faces = faces;
    this.colors = colors ?? this.colors;
    textureCoordinates = texture_coords ?? textureCoordinates;
    this.applicationId = applicationId;
    this.units = units;
  }

  [Obsolete("Use lists constructor", true)]
  public Mesh(
    double[] vertices,
    int[] faces,
    int[]? colors = null,
    double[]? texture_coords = null,
    string units = Units.Meters,
    string? applicationId = null
  )
    : this(
      vertices.ToList(),
      faces.ToList(),
      colors?.ToList() ?? new(),
      texture_coords?.ToList() ?? new(),
      units,
      applicationId
    ) { }

  [DetachProperty, Chunkable(31250)]
  public List<double> vertices { get; set; } = new();

  [DetachProperty, Chunkable(62500)]
  public List<int> faces { get; set; } = new();

  /// <summary> Vertex colors as ARGB <see cref="int"/>s</summary>
  [DetachProperty, Chunkable(62500)]
  public List<int> colors { get; set; } = new();

  [DetachProperty, Chunkable(31250)]
  public List<double> textureCoordinates { get; set; } = new();

  /// <summary>
  /// The unit's this <see cref="Mesh"/> is in.
  /// This should be one of <see cref="Speckle.Core.Kits.Units"/>
  /// </summary>
  public string units { get; set; } = Units.None;

  /// <inheritdoc/>
  public double area { get; set; }

  /// <inheritdoc/>
  public Box bbox { get; set; }

  /// <inheritdoc/>
  public double volume { get; set; }

  /// <inheritdoc/>
  public bool Transform(Transform transform)
  {
    // transform vertices
    vertices = GetPoints()
      .SelectMany(vertex =>
      {
        vertex.TransformTo(transform, out Point transformedVertex);
        return transformedVertex.ToList();
      })
      .ToList();

    return true;
  }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out Mesh transformed)
  {
    // transform vertices
    var transformedVertices = new List<Point>();
    foreach (var vertex in GetPoints())
    {
      vertex.TransformTo(transform, out Point transformedVertex);
      transformedVertices.Add(transformedVertex);
    }

    transformed = new Mesh
    {
      vertices = transformedVertices.SelectMany(o => o.ToList()).ToList(),
      textureCoordinates = textureCoordinates,
      applicationId = applicationId ?? id,
      faces = faces,
      colors = colors,
      units = units
    };
    transformed["renderMaterial"] = this["renderMaterial"];

    return true;
  }

  /// <inheritdoc/>
  public bool TransformTo(Transform transform, out ITransformable transformed)
  {
    var res = TransformTo(transform, out Mesh brep);
    transformed = brep;
    return res;
  }

  #region Convenience Methods

  [JsonIgnore]
  public int VerticesCount => vertices.Count / 3;

  [JsonIgnore]
  public int TextureCoordinatesCount => textureCoordinates.Count / 2;

  /// <summary>
  /// Gets a vertex as a <see cref="Point"/> by <paramref name="index"/>
  /// </summary>
  /// <param name="index">The index of the vertex</param>
  /// <returns>Vertex as a <see cref="Point"/></returns>
  public Point GetPoint(int index)
  {
    index *= 3;
    return new Point(vertices[index], vertices[index + 1], vertices[index + 2], units, applicationId);
  }

  /// <returns><see cref="vertices"/> as list of <see cref="Point"/>s</returns>
  /// <exception cref="SpeckleException">when list is malformed</exception>
  public List<Point> GetPoints()
  {
    if (vertices.Count % 3 != 0)
    {
      throw new SpeckleException(
        $"{nameof(Mesh)}.{nameof(vertices)} list is malformed: expected length to be multiple of 3"
      );
    }

    var pts = new List<Point>(vertices.Count / 3);
    for (int i = 2; i < vertices.Count; i += 3)
    {
      pts.Add(new Point(vertices[i - 2], vertices[i - 1], vertices[i], units));
    }

    return pts;
  }

  /// <summary>
  /// Gets a texture coordinate as a <see cref="ValueTuple{T1, T2}"/> by <paramref name="index"/>
  /// </summary>
  /// <param name="index">The index of the texture coordinate</param>
  /// <returns>Texture coordinate as a <see cref="ValueTuple{T1, T2}"/></returns>
  public (double, double) GetTextureCoordinate(int index)
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
  /// to ensure compatibility with geometry originating from applications that map <see cref="Mesh.vertices"/> to <see cref="Mesh.textureCoordinates"/> using vertex instance index (rather than vertex index)
  /// <br/>
  /// <see cref="Mesh.vertices"/>, <see cref="Mesh.colors"/>, and <see cref="faces"/> lists will be modified to contain no shared vertices (vertices shared between polygons)
  /// </remarks>
  public void AlignVerticesWithTexCoordsByIndex()
  {
    if (textureCoordinates.Count == 0)
    {
      return;
    }

    if (TextureCoordinatesCount == VerticesCount)
    {
      return; //Tex-coords already aligned as expected
    }

    var facesUnique = new List<int>(faces.Count);
    var verticesUnique = new List<double>(TextureCoordinatesCount * 3);
    bool hasColors = colors.Count > 0;
    var colorsUnique = hasColors ? new List<int>(TextureCoordinatesCount) : null;

    int nIndex = 0;
    while (nIndex < faces.Count)
    {
      int n = faces[nIndex];
      if (n < 3)
      {
        n += 3; // 0 -> 3, 1 -> 4
      }

      if (nIndex + n >= faces.Count)
      {
        break; //Malformed face list
      }

      facesUnique.Add(n);
      for (int i = 1; i <= n; i++)
      {
        int vertIndex = faces[nIndex + i];
        int newVertIndex = verticesUnique.Count / 3;

        var (x, y, z) = GetPoint(vertIndex);
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
