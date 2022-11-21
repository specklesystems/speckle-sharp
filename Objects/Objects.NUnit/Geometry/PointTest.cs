using NUnit.Framework;
using Objects.Geometry;
using Objects.Other;

namespace Objects.NUnit.Geometry;

public class PointTest
{
  [SetUp]
  public void Setup()
  {
  }

  [TestCase(1, 2, 3)]
  [TestCase(4, 5, 6)]
  public void TestInitPoint(double x, double y, double z)
  {
    Point point = new Point(x, y, z);
    Assert.NotNull(point);
    Assert.That(point.x, Is.EqualTo(x));
    Assert.That(point.y, Is.EqualTo(y));
    Assert.That(point.z, Is.EqualTo(z));
    Assert.That(point.units, Is.EqualTo("m"));
  }

  [TestCase(0, 1, 2)]
  [TestCase(4, 6, 7)]
  public void TestPointToList(double x, double y, double z)
  {
    Point point = new Point(x, y, z);
    List<double> doubles = point.ToList();
    Assert.That(doubles[0], Is.EqualTo(x));
    Assert.That(doubles[1], Is.EqualTo(y));
    Assert.That(doubles[2], Is.EqualTo(z));
  }

  [TestCase(4, 5, 6)]
  [TestCase(1, 2, 3)]
  public void TestPointFromList(params double[] list)
  {
    Point point = Point.FromList(list, "m");
    Assert.That(point.x, Is.EqualTo(list[0]));
    Assert.That(point.y, Is.EqualTo(list[1]));
    Assert.That(point.z, Is.EqualTo(list[2]));
  }

  [TestCase(1, 1, 1)]
  [TestCase(3, 4, 5)]
  [TestCase(4, 5, 1)]
  public void TestDeconstruct(double x, double y, double z)
  {
    Point point = new Point(x, y, z);
    point.Deconstruct(out double x1, out double y1, out double z1);
    Assert.That(x1, Is.EqualTo(x));
    Assert.That(y1, Is.EqualTo(y));
    Assert.That(z1, Is.EqualTo(z));
  }

  [TestCase(1, 1, 1)]
  public void TestTransformTo(double x,double y,double z)
  {
    Point point = new Point(x, y, z);
    Transform transform = new Transform();
    point.TransformTo(transform, out Point point1);
    Assert.That(point1.x, Is.EqualTo(x));
    Assert.That(point1.y, Is.EqualTo(y));
    Assert.That(point1.z, Is.EqualTo(z));
  }

  [Test]
  public void TestPointPlus()
  {
    Point point1 = new Point(1, 2, 3);
    Point point2 = new Point(4, 5, 6);
    Point point3 = point1 + point2;
    Assert.That(point3.x, Is.EqualTo(5));
    Assert.That(point3.y, Is.EqualTo(7));
    Assert.That(point3.z, Is.EqualTo(9));
  }

  [Test]
  public void TestPointSubtract()
  {
    Point point1 = new Point(1, 2, 3);
    Point point2 = new Point(4, 5, 6);
    Point point3 = point1 - point2;
    Assert.That(point3.x, Is.EqualTo(-3));
    Assert.That(point3.y, Is.EqualTo(-3));
    Assert.That(point3.z, Is.EqualTo(-3));
  }

  [Test]
  public void TestPointDivide()
  {
    Point point1 = new Point(1, 2, 3);
    Point point = point1 / 2;
    Assert.That(point.x, Is.EqualTo(0.5));
    Assert.That(point.y, Is.EqualTo(1));
    Assert.That(point.z, Is.EqualTo(1.5));
  }

  [Test]
  public void TestPointMultiply()
  {
    Point point1 = new Point(1, 2, 3);
    Point point = point1 * 2;
    Assert.That(point.x, Is.EqualTo(2));
    Assert.That(point.y, Is.EqualTo(4));
    Assert.That(point.z, Is.EqualTo(6));
  }

  [Test]
  public void TestMidPoint()
  {
    Point point1 = new Point(1, 2, 3);
    Point point2 = new Point(4, 5, 6);
    Point point3 = Point.Midpoint(point1, point2);
    Assert.That(point3.x, Is.EqualTo(2.5));
    Assert.That(point3.y, Is.EqualTo(3.5));
    Assert.That(point3.z, Is.EqualTo(4.5));
  }

  [Test]
  public void TestDistance()
  {
    Point point1 = new Point(1, 2, 3);
    Point point2 = new Point(4, 5, 6);
    double distance = Point.Distance(point1, point2);
    double distanceTo = point1.DistanceTo(point2);
    Assert.That(distanceTo, Is.EqualTo(5.196152422706632));
    Assert.That(distance, Is.EqualTo(5.196152422706632));
  }
}