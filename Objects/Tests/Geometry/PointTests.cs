using NUnit.Framework;
using Objects.Geometry;

namespace Objects.Tests.Geometry
{
    [TestFixture, TestOf(typeof(Point))]
    public class PointTests
    {
        
        [Test]
        public void TestNull()
        {
            Point a = null;
            Point b = null;
            Point c = new Point(0,0,0,null);
            
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(b == a);
            Assert.False(b != a);
            
            Assert.False(a == c);
            Assert.True(a != c);
            Assert.False(c == a);
            Assert.True(c != a);
        }
        
        [Test]
        [TestCase(1, 1, 1, "m", 1, 1, 1, "m", true)] 
        [TestCase(1, 1, 1, "m", 0, 1, 1, "m", false)]
        [TestCase(1, 1, 1, "m", 1, 0, 1, "m", false)]
        [TestCase(1, 1, 1, "m", 1, 1, 0, "m", false)]
        // Units
        [TestCase(1, 1, 1, "", 1, 1, 1, "", true)]
        [TestCase(1, 1, 1, null, 1, 1, 1, null, true)]
        [TestCase(1, 1, 1, "m", 1, 1, 1, "meters", false)] 
        [TestCase(1, 1, 1, "m", 1, 1, 1, "M", false)]
        public void TestNotEqual(
            double x1, double y1, double z1, string units1,
            double x2, double y2, double z2, string units2,
            bool expectEquality)
        {
            Point p1 = new Point(x1, y1, z1, units1);
            Point p2 = new Point(x2, y2, z2, units2);
            
            Assert.AreEqual(p1 == p2, expectEquality);
            Assert.AreEqual(p2 == p1, expectEquality);
        }
    }
}
