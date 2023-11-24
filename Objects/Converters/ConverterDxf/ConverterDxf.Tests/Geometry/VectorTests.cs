using System.Collections.Generic;
using Objects.Geometry;
using Xunit;
using Dxf = Speckle.netDxf;
using Dxfe = Speckle.netDxf.Entities;

namespace ConverterDxf.Tests.Geometry
{
  public class VectorTests : IClassFixture<ConverterFixture>
  {
    private readonly ConverterFixture fixture = new ConverterFixture();

    public static IEnumerable<object[]> PointData => ConverterSetup.GetTestMemberData<Point>();
    public static IEnumerable<object[]> VectorData => ConverterSetup.GetTestMemberData<Vector>();

    [Theory]
    [MemberData(nameof(PointData))]
    public void CanConvert_PointToNative(Point pt)
    {
      var dxfPt = fixture.AssertAndConvertToNative<Dxfe.Point>(pt);
      Assert.Equal(pt.x, dxfPt.Position.X);
      Assert.Equal(pt.y, dxfPt.Position.Y);
      Assert.Equal(pt.z, dxfPt.Position.Z);
    }

    [Theory]
    [MemberData(nameof(VectorData))]
    public void CanConvert_VectorToNative(Vector v)
    {
      var dxfVector = fixture.AssertAndConvertToNative<Dxf.Vector3>(v);
      Assert.Equal(v.x, dxfVector.X);
      Assert.Equal(v.y, dxfVector.Y);
      Assert.Equal(v.z, dxfVector.Z);
    }
  }
}
