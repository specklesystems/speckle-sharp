using System;
using System.Collections.Generic;
using System.IO;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Xunit;

namespace ConverterRhinoGhTests
{
    /// <summary>
    /// XUnit tests
    /// </summary>
    [Collection("RhinoTestingCollection")]
    public class RhinoMeshTest
    {
        RhinoTestFixture _fixture;
        private RhinoDoc doc;
        private ISpeckleConverter Converter;
        
        public RhinoMeshTest(RhinoTestFixture fixture)
        {
            _fixture = fixture;
            var kit = KitManager.GetDefaultKit();
            Converter = kit.LoadConverter(HostApplications.Rhino.GetVersion(HostAppVersion.v7));
            doc = RhinoDoc.Create("meters");
            Converter.SetContextDocument(doc);
        }
        
        public static IEnumerable<object[]> MeshData =>
            new List<Mesh[]>
            {
                new [] { Rhino.Geometry.Mesh.CreateFromBox( new Rhino.Geometry.BoundingBox(new Point3d(0, 0, 0), new Point3d(100, 100, 100) ),10, 10, 10 ) }
                
            };
        
        [Theory]
        [MemberData(nameof(MeshData))]
        public void Mesh_ToSpeckle(Mesh mesh)
        {
            Assert.True(Converter.CanConvertToSpeckle(mesh));
            
            var result = Converter.ConvertToSpeckle(mesh);
            
            Assert.NotNull(result);
            Assert.IsType<Objects.Geometry.Mesh>(result);
            Assert.Empty(Converter.Report.ConversionErrors);
        }
    }
}