using NUnit.Framework;
using Objects.Geometry;

namespace Objects.Tests.Unit.Geometry;

[TestFixture, TestOf(typeof(Mesh))]
public class MeshTests
{
  private static Mesh[] s_testCaseSource = { CreateBlenderStylePolygon(), CreateRhinoStylePolygon() };

  [Test, TestCaseSource(nameof(s_testCaseSource))]
  public void CanAlignVertices(Mesh inPolygon)
  {
    inPolygon.AlignVerticesWithTexCoordsByIndex();

    Assert.That(inPolygon.VerticesCount, Is.EqualTo(inPolygon.TextureCoordinatesCount));

    var expectedPolygon = CreateRhinoStylePolygon();

    Assert.That(inPolygon.vertices, Is.EquivalentTo(expectedPolygon.vertices));
    Assert.That(inPolygon.faces, Is.EquivalentTo(expectedPolygon.faces));
    Assert.That(inPolygon.textureCoordinates, Is.EquivalentTo(expectedPolygon.textureCoordinates));
  }

  private static Mesh CreateRhinoStylePolygon()
  {
    return new Mesh
    {
      vertices = { 0, 0, 0, 0, 0, 1, 1, 0, 1, 0, 0, 0, 1, 0, 1, 1, 0, 0 },
      faces = { 3, 0, 1, 2, 3, 3, 4, 5 },
      textureCoordinates = { 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0 }
    };
  }

  private static Mesh CreateBlenderStylePolygon()
  {
    return new Mesh
    {
      vertices = { 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 0 },
      faces = { 3, 0, 1, 2, 3, 0, 2, 3 },
      textureCoordinates = { 0, 0, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0 }
    };
  }
}
