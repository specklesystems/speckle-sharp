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
    public class XunitExampleTests 
    {
        RhinoTestFixture _fixture;
        private RhinoDoc doc;
        private ISpeckleConverter Converter;
        
        public XunitExampleTests(RhinoTestFixture fixture)
        {
           _fixture = fixture;
           var kit = KitManager.GetDefaultKit();
           Converter = kit.LoadConverter(HostApplications.Rhino.GetVersion(HostAppVersion.v7));
           doc = RhinoDoc.Create("meters");
           Converter.SetContextDocument(doc);
        }
        
        public static IEnumerable<object[]> BrepData =>
            new List<Brep[]>
            {
                new [] { new BoundingBox(new Point3d(0, 0, 0), new Point3d(100, 100, 100)).ToBrep() },
                new [] { new Sphere(Plane.WorldXY, 4).ToBrep() },
                new [] { Surface.CreateExtrusion(
                    new Line(new Point3d(0,0,0), new Point3d(1,1,1)).ToNurbsCurve(),
                    Vector3d.ZAxis)
                    .ToBrep()
                }
            };
        
        [Theory]
        [MemberData(nameof(BrepData))]
        public void Brep_ToSpeckle(Brep brep)
        {
            Assert.True(Converter.CanConvertToSpeckle(brep));
            
            var result = Converter.ConvertToSpeckle(brep);
            
            Assert.NotNull(result);
            Assert.IsType<Objects.Geometry.Brep>(result);
            Assert.Empty(Converter.Report.ConversionErrors);
        }
    }
}