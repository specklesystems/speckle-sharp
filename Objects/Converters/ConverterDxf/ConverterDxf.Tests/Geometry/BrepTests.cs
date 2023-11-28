using System.Collections.Generic;
using System.Linq;
using Objects.Geometry;
using Xunit;
using Dxf = Speckle.netDxf;
using Dxfe = Speckle.netDxf.Entities;

namespace ConverterDxf.Tests.Geometry
{
  public class BrepTests : IClassFixture<ConverterFixture>
  {
    private readonly ConverterFixture fixture = new ConverterFixture();

    public static IEnumerable<object[]> BrepData => ConverterSetup.GetTestMemberData<Brep>();

    [Theory]
    [MemberData(nameof(BrepData))]
    public void CanConvert_BrepToPrettyNative(Brep mesh)
    {
      fixture.Converter.Settings.PrettyMeshes = true;
      var dxfMeshEntities = fixture.AssertAndConvertToNative<IEnumerable<Dxfe.EntityObject>>(mesh).ToList();
      Assert.NotEmpty(dxfMeshEntities);
      Assert.All(dxfMeshEntities, o => Assert.True(o is Dxfe.Face3D || o is Dxfe.Line));
      // TODO: Add better tests for the BREP fallback to mesh
    }

    [Theory]
    [MemberData(nameof(BrepData))]
    public void CanConvert_BrepToNative(Brep mesh)
    {
      fixture.Converter.Settings.PrettyMeshes = false;
      var dxfMeshes = fixture.AssertAndConvertToNative<IEnumerable<Dxfe.Mesh>>(mesh);
      Assert.NotEmpty(dxfMeshes);
      // TODO: Add better tests for the BREP fallback to mesh
    }
  }
}
