using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Objects.Geometry;

namespace Objects.Tests.Unit.Geometry;

[TestFixture, TestOf(typeof(Point))]
public class PointTests
{
  [Test]
  [SuppressMessage(
    "Assertion",
    "NUnit2010:Use EqualConstraint for better assertion messages in case of failure",
    Justification = "Need to explicitly test equality operator"
  )]
  public void TestNull()
  {
    Point a = null;
    Point b = null;
    Point c = new(0, 0, 0, null);

    Assert.Multiple(() =>
    {
      Assert.That(a == b, Is.True);
      Assert.That(a != b, Is.False);
      Assert.That(b == a, Is.True);
      Assert.That(b != a, Is.False);

      Assert.That(a == c, Is.False);
      Assert.That(a != c, Is.True);
      Assert.That(c == a, Is.False);
      Assert.That(c != a, Is.True);
    });
  }

  [Test]
  [TestCase(1, 1, 1, "m", 1, 1, 1, "m", ExpectedResult = true)]
  [TestCase(1, 1, 1, "m", 0, 1, 1, "m", ExpectedResult = false)]
  [TestCase(1, 1, 1, "m", 1, 0, 1, "m", ExpectedResult = false)]
  [TestCase(1, 1, 1, "m", 1, 1, 0, "m", ExpectedResult = false)]
  [TestCase(1, 1, 1, "", 1, 1, 1, "", ExpectedResult = true)]
  [TestCase(1, 1, 1, null, 1, 1, 1, null, ExpectedResult = true)]
  [TestCase(1, 1, 1, "m", 1, 1, 1, "meters", ExpectedResult = false)]
  [TestCase(1, 1, 1, "m", 1, 1, 1, "M", ExpectedResult = false)]
  // Units
  public bool TestEqual(double x1, double y1, double z1, string units1, double x2, double y2, double z2, string units2)
  {
    Point p1 = new(x1, y1, z1, units1);
    Point p2 = new(x2, y2, z2, units2);

    return p1 == p2;
  }
}
