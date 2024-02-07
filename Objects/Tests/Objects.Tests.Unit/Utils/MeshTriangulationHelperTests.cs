using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Objects.Geometry;
using Objects.Utils;

namespace Objects.Tests.Unit.Utils;

[TestFixture, TestOf(typeof(MeshTriangulationHelper))]
public class MeshTriangulationHelperTests
{
  [Test]
  public void PolygonTest([Range(3, 9)] int n, [Values] bool planar)
  {
    //Test Setup
    List<double> vertices = new(n) { 0, planar ? 0 : 1, 1 };
    for (int i = 1; i < n; i++)
    {
      vertices.Add(i);
      vertices.Add(0);
      vertices.Add(0);
    }

    List<int> faces = new(n + 1) { n };
    faces.AddRange(Enumerable.Range(0, n));

    Mesh mesh = new(vertices, faces);

    //Test
    mesh.TriangulateMesh();

    //Results
    int numExpectedTriangles = n - 2;
    int expectedFaceCount = numExpectedTriangles * 4;

    Assert.That(mesh.faces, Has.Count.EqualTo(expectedFaceCount));
    for (int i = 0; i < expectedFaceCount; i += 4)
    {
      Assert.That(mesh.faces[i], Is.EqualTo(3));
      Assert.That(mesh.faces.GetRange(i + 1, 3), Is.Unique);
    }

    Assert.That(mesh.faces, Is.SupersetOf(Enumerable.Range(0, n)));

    Assert.That(mesh.faces, Is.All.GreaterThanOrEqualTo(0));
    Assert.That(mesh.faces, Is.All.LessThan(Math.Max(n, 4)));
  }

  [Test]
  public void DoesntFlipNormals()
  {
    //Test Setup
    List<double> vertices = new() { 0, 0, 0, 1, 0, 0, 1, 0, 1 };

    List<int> faces = new() { 3, 0, 1, 2 };

    Mesh mesh = new(vertices, new List<int>(faces));

    //Test
    mesh.TriangulateMesh();

    //Results

    List<int> shift1 = faces;
    List<int> shift2 = new() { 3, 1, 2, 0 };
    List<int> shift3 = new() { 3, 2, 0, 1 };

    Assert.That(mesh.faces, Is.AnyOf(shift1, shift2, shift3));
  }

  [Test]
  public void PreserveQuads([Values] bool preserveQuads)
  {
    //Test Setup
    List<double> vertices = new() { 0, 0, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1 };

    List<int> faces = new() { 4, 0, 1, 2, 3 };

    Mesh mesh = new(vertices, new List<int>(faces));

    //Test
    mesh.TriangulateMesh(preserveQuads);

    //Results
    int expectedN = preserveQuads ? 4 : 3;
    int expectedFaceCount = preserveQuads ? 5 : 8;

    Assert.That(mesh.faces, Has.Count.EqualTo(expectedFaceCount));
    Assert.That(mesh.faces[0], Is.EqualTo(expectedN));
  }
}
