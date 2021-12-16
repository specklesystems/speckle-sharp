using System.Collections.Generic;
using NUnit.Framework;
using Objects.Geometry;
using Objects.Utils;

namespace Tests.Utils
{
    
    [TestFixture, TestOf(typeof(MeshTriangulationHelper))]
    public class MeshTriangulationHelperTests
    {
        [Test]
        public void PlanarQuads()
        {
            //Test Setup
            List<double> vertices = new()
            {
                0, 0, 0,
                1, 0, 0,
                1, 0, 1,
                0, 0, 1,
            };

            List<int> faces = new()
            {
                4, 0, 1, 2, 3
            };
            Mesh mesh = new(vertices, faces);
            
            //Test
            mesh.TriangulateMesh();
            
            //Results
            Assert.That(mesh.faces, Has.Count.EqualTo(4 * 2));
            Assert.That(mesh.faces[0],Is.EqualTo(3));
            Assert.That(mesh.faces[4], Is.EqualTo(3));
            Assert.That(mesh.faces.GetRange(1,3), Is.Unique); //Check first triangle has all uniq
            Assert.That(mesh.faces.GetRange(5,3), Is.Unique); //Check second triangle has all uniq
            Assert.That(mesh.faces, Is.All.GreaterThanOrEqualTo(0));
            Assert.That(mesh.faces, Is.All.LessThan(4));
        }
        
        
    }
}