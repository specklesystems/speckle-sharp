using System;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Objects.Geometry;

namespace Tests.Geometry
{
  [TestFixture, TestOf(typeof(Arc))]

  public class ArcTests
  {
    public Plane TestPlane =>  new (
    new Point(0,0),
    new Vector(0,0,1),
    new Vector(1,0,0),
    new Vector(0,1,0)
      );
    
    [Test]
    public void CanCreateArc_HalfCircle()
    {
      var arc = new Arc(
        TestPlane,
        new Point(-5,5),
        new Point(5,5),
        Math.PI
      );

      Assert.AreEqual(arc.startAngle, 0);
      Assert.AreEqual(arc.endAngle, Math.PI);
      
      Assert.True(Point.Distance(arc.midPoint, new Point(0,0)) <= 0.0001);
      Assert.True(Point.Distance(arc.plane.origin, new Point(0,5)) <= 0.0001);
    }
  }
}