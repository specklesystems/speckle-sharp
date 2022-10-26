using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Xunit;
using Dxf = Speckle.netDxf;
using Dxfe = Speckle.netDxf.Entities;

namespace ConverterDxf.Tests.Geometry
{
    public class MeshTests : IClassFixture<ConverterFixture>
    {
        private readonly ConverterFixture fixture = new ConverterFixture();

        public static IEnumerable<object[]> MeshData => ConverterSetup.GetTestMemberData<Mesh>();

        [Theory]
        [MemberData(nameof(MeshData))]
        public void CanConvert_MeshToPrettyNative(Mesh mesh)
        {
            fixture.Converter.Settings.PrettyMeshes = true;
            var dxfMeshEntities = fixture.AssertAndConvertToNative<IEnumerable<Dxfe.EntityObject>>(mesh).ToList();
            Assert.NotEmpty(dxfMeshEntities);
            Assert.All(dxfMeshEntities, o => Assert.True(o is Dxfe.Face3D || o is Dxfe.Line));
        }
        
        [Theory]
        [MemberData(nameof(MeshData))]
        public void CanConvert_MeshToNative(Mesh mesh)
        {
            fixture.Converter.Settings.PrettyMeshes = false;
            var dxfMesh = fixture.AssertAndConvertToNative<Dxfe.Mesh>(mesh);
            //TODO: Add mesh specific tests
            Assert.Equal(mesh.vertices.Count, dxfMesh.Vertexes.Count * 3);
        }

    }
}