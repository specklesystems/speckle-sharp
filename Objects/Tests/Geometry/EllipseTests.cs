using System;
using SN = System.Numerics;
using NUnit.Framework;
using Objects.Geometry;
using Objects.Other;

namespace Objects.Tests.Geometry
{
  [TestFixture, TestOf(typeof(Ellipse))]
  internal class EllipseTests
  {
    private const float FLOAT_TOLLERANCE = 1e-6f;
    [Test]
    public void TestFlattenEllipse()
    {
      var cos30 = Math.Cos(30 * Math.PI / 180);
      var sin30 = Math.Sin(30 * Math.PI / 180);

      var newZ = 5;

      var plane = new Plane(
        new Point(6, 11, 10),
        new Vector(-sin30, 0, cos30), // 30 degree rotation in the xz plane
        new Vector(cos30, 0, sin30),
        new Vector(0, 1, 0)
      );

      var ellipse = new Ellipse(plane, 10, 5);

      var flatPlane = new Plane(
        new Point(plane.origin.x, plane.origin.y, newZ),
        new Vector(0, 0, 1),
        new Vector(1, 0, 0),
        new Vector(0, 1, 0)
      );

      var expectedEllipse = new Ellipse(flatPlane, 10 * cos30, 5);

      var flattenTransform = new Transform(
        new Vector(1, 0, 0),
        new Vector(0, 1, 0),
        new Vector(0, 0, 0),
        new Vector(0, 0, flatPlane.origin.z)
      );

      _ = ellipse.plane.TransformTo(flattenTransform, out Plane newEllipsePlane);

      newEllipsePlane.xdir.Normalize();
      newEllipsePlane.ydir.Normalize();
      newEllipsePlane.normal = Vector.CrossProduct(newEllipsePlane.xdir, newEllipsePlane.ydir);

      var rad1Scale = Vector.DotProduct(ellipse.plane.xdir, newEllipsePlane.xdir) / (ellipse.plane.xdir.Length * newEllipsePlane.xdir.Length);

      var rad2Scale = Vector.DotProduct(ellipse.plane.ydir, newEllipsePlane.ydir) / (ellipse.plane.ydir.Length * newEllipsePlane.ydir.Length);

      var newEllipse = new Ellipse(
        newEllipsePlane,
        (ellipse.firstRadius ?? 0) * rad1Scale,
        (ellipse.secondRadius ?? 0) * rad2Scale
      );

      Assert.AreEqual(newEllipse.plane.xdir.x, expectedEllipse.plane.xdir.x, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.xdir.y, expectedEllipse.plane.xdir.y, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.xdir.z, expectedEllipse.plane.xdir.z, FLOAT_TOLLERANCE);
      
      Assert.AreEqual(newEllipse.plane.ydir.x, expectedEllipse.plane.ydir.x, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.ydir.y, expectedEllipse.plane.ydir.y, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.ydir.z, expectedEllipse.plane.ydir.z, FLOAT_TOLLERANCE);
      
      Assert.AreEqual(newEllipse.plane.normal.x, expectedEllipse.plane.normal.x, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.normal.y, expectedEllipse.plane.normal.y, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.normal.z, expectedEllipse.plane.normal.z, FLOAT_TOLLERANCE);
      
      Assert.AreEqual(newEllipse.plane.origin.x, expectedEllipse.plane.origin.x, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.origin.y, expectedEllipse.plane.origin.y, FLOAT_TOLLERANCE);
      Assert.AreEqual(newEllipse.plane.origin.z, expectedEllipse.plane.origin.z, FLOAT_TOLLERANCE);

      Assert.AreEqual(newEllipse.firstRadius, expectedEllipse.firstRadius);
      Assert.AreEqual(newEllipse.secondRadius, expectedEllipse.secondRadius);
    }
  }
}
