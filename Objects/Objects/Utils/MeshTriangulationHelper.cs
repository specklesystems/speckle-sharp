using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Objects.Geometry;

namespace Objects.Utils;

/// <summary>
/// Set of functions to triangulate n-gon faces (i.e. polygon faces with an arbitrary (n) number of vertices) in <see cref="Mesh"/>es.
/// </summary>
public static class MeshTriangulationHelper
{
  /// <summary>
  /// Triangulates all faces in <paramref name="mesh"/>.
  /// </summary>
  /// <param name="mesh">The mesh to triangulate.</param>
  /// <param name="preserveQuads">If <see langword="true"/>, will not triangulate quad faces.</param>
  public static void TriangulateMesh(this Mesh mesh, bool preserveQuads = false)
  {
    List<int> triangles = new(mesh.faces.Count); //Our new list is going to be at least as big as our old one
    int i = 0;
    while (i < mesh.faces.Count)
    {
      int n = mesh.faces[i];
      if (n < 3)
      {
        n += 3; // 0 -> 3, 1 -> 4
      }

      if (n == 3)
      {
        triangles.Add(3);
        triangles.Add(mesh.faces[i + 1]);
        triangles.Add(mesh.faces[i + 2]);
        triangles.Add(mesh.faces[i + 3]);
      }
      else if (preserveQuads && n == 4)
      {
        triangles.Add(4);
        triangles.Add(mesh.faces[i + 1]);
        triangles.Add(mesh.faces[i + 2]);
        triangles.Add(mesh.faces[i + 3]);
        triangles.Add(mesh.faces[i + 4]);
      }
      else
      {
        var triangle = TriangulateFace(i, mesh);
        triangles.AddRange(triangle);
      }

      i += n + 1;
    }

    mesh.faces = triangles;
  }

  /// <overloads>Overload using a <see cref="Mesh"/>, does not mutate <paramref name="mesh"/></overloads>
  /// <inheritdoc cref="TriangulateFace(int,IReadOnlyList{int},IReadOnlyList{double},bool)"/>
  public static List<int> TriangulateFace(int faceIndex, Mesh mesh, bool includeIndicators = true)
  {
    return TriangulateFace(faceIndex, mesh.faces, mesh.vertices, includeIndicators);
  }

  /// <summary>
  /// Calculates the triangulation of the face at <paramref name="faceIndex"/> in <paramref name="faces"/> list.
  /// </summary>
  /// <remarks>
  /// This implementation is based the ear clipping method
  /// Proposed by "Christer Ericson (2005) <i>Real-Time Collision Detection</i>".
  /// </remarks>
  /// <param name="faceIndex">The index of the face's cardinality indicator <c>n</c> in <paramref name="faces"/> list</param>.
  /// <param name="faces"></param>
  /// <param name="vertices"></param>
  /// <param name="includeIndicators">if <see langword="true"/>, the returned list will include cardinality indicators for each triangle
  /// (i.e 4 ints for each tri), otherwise will simply be 3 ints for each tri.</param>
  /// <returns>List of triangle faces in the specified format.</returns>
  public static List<int> TriangulateFace(
    int faceIndex,
    IReadOnlyList<int> faces,
    IReadOnlyList<double> vertices,
    bool includeIndicators = true
  )
  {
    int n = faces[faceIndex];
    if (n < 3)
    {
      n += 3; // 0 -> 3, 1 -> 4
    }
    #region Local Funcitions
    //Converts from relative to absolute index (returns index in mesh.vertices list)
    int AsIndex(int v) => faceIndex + v + 1;

    //Gets vertex from a relative vert index
    Vector3 V(int v)
    {
      int index = faces[AsIndex(v)] * 3;
      return new Vector3(vertices[index], vertices[index + 1], vertices[index + 2]);
    }
    #endregion

    int intsPerTri = includeIndicators ? 4 : 3;
    List<int> triangleFaces = new((n - 2) * intsPerTri);

    //Calculate face normal using the Newell Method
    Vector3 faceNormal = Vector3.Zero;
    for (int ii = n - 1, jj = 0; jj < n; ii = jj, jj++)
    {
      Vector3 iPos = V(ii);
      Vector3 jPos = V(jj);
      faceNormal.x += (jPos.y - iPos.y) * (iPos.z + jPos.z); // projection on yz
      faceNormal.y += (jPos.z - iPos.z) * (iPos.x + jPos.x); // projection on xz
      faceNormal.z += (jPos.x - iPos.x) * (iPos.y + jPos.y); // projection on xy
    }
    faceNormal.Normalize();

    //Set up previous and next links to effectively form a double-linked vertex list
    int[] prev = new int[n],
      next = new int[n];
    for (int j = 0; j < n; j++)
    {
      prev[j] = j - 1;
      next[j] = j + 1;
    }
    prev[0] = n - 1;
    next[n - 1] = 0;

    //Start clipping ears until we are left with a triangle
    int i = 0;
    int counter = 0;
    while (n >= 3)
    {
      bool isEar = true;

      //If we are the last triangle or we have exhausted our vertices, the below statement will be false
      if (n > 3 && counter < n)
      {
        Vector3 prevVertex = V(prev[i]);
        Vector3 earVertex = V(i);
        Vector3 nextVertex = V(next[i]);

        if (TriangleIsCCW(faceNormal, prevVertex, earVertex, nextVertex))
        {
          int k = next[next[i]];

          do
          {
            if (TestPointTriangle(V(k), prevVertex, earVertex, nextVertex))
            {
              isEar = false;
              break;
            }

            k = next[k];
          } while (k != prev[i]);
        }
        else
        {
          isEar = false;
        }
      }

      if (isEar)
      {
        int a = faces[AsIndex(i)];
        int b = faces[AsIndex(next[i])];
        int c = faces[AsIndex(prev[i])];

        if (includeIndicators)
        {
          triangleFaces.Add(3);
        }

        triangleFaces.Add(a);
        triangleFaces.Add(b);
        triangleFaces.Add(c);

        next[prev[i]] = next[i];
        prev[next[i]] = prev[i];
        n--;
        i = prev[i];
        counter = 0;
      }
      else
      {
        i = next[i];
        counter++;
      }
    }

    return triangleFaces;
  }

  /// <summary>
  /// Tests if point <paramref name="v"/> is within triangle <paramref name="a"/><paramref name="b"/><paramref name="c"/>
  /// </summary>
  /// <returns>true if <paramref name="v"/> is within triangle</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool TestPointTriangle(Vector3 v, Vector3 a, Vector3 b, Vector3 c)
  {
    static bool Test(Vector3 v, Vector3 a, Vector3 b)
    {
      Vector3 crossA = v.Cross(a);
      Vector3 crossB = a.Cross(b);
      double dotWithEpsilon = double.Epsilon + crossA.Dot(crossB);
      return Math.Sign(dotWithEpsilon) != -1;
    }

    return Test(b - a, v - a, c - a) && Test(c - b, v - b, a - b) && Test(a - c, v - c, b - c);
  }

  /// <summary>
  /// Checks that triangle <paramref name="a"/><paramref name="b"/><paramref name="c"/> is clockwise with reference to <paramref name="referenceNormal"/>
  /// </summary>
  /// <param name="referenceNormal">The normal direction of the face</param>
  /// <param name="a"></param>
  /// <param name="b"></param>
  /// <param name="c"></param>
  /// <returns>true if triangle is ccw</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool TriangleIsCCW(Vector3 referenceNormal, Vector3 a, Vector3 b, Vector3 c)
  {
    Vector3 triangleNormal = (c - a).Cross(b - a);
    triangleNormal.Normalize();
    return referenceNormal.Dot(triangleNormal) > 0.0f;
  }

  /// <summary>
  /// 3-dimension x, Y, Z Vector of <see cref="double"/>s encapsulating necessary vector mathematics
  /// </summary>
  private struct Vector3
  {
    public double x,
      y,
      z;

    public Vector3(double x, double y, double z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    public static readonly Vector3 Zero = new(0, 0, 0);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);

    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);

    public readonly double Dot(Vector3 v)
    {
      return x * v.x + y * v.y + z * v.z;
    }

    public readonly Vector3 Cross(Vector3 v)
    {
      var x = this.y * v.z - this.z * v.y;
      var y = this.z * v.x - this.x * v.z;
      var z = this.x * v.y - this.y * v.x;

      return new Vector3(x, y, z);
    }

    public readonly double SquareSum => x * x + y * y + z * z;

    public void Normalize()
    {
      double scale = 1d / Math.Sqrt(SquareSum);
      x *= scale;
      y *= scale;
      z *= scale;
    }
  }
}
