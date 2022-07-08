using System.Collections.Generic;
using Objects.Geometry;
using Xunit;
using Dxf = netDxf;
using Dxfe = netDxf.Entities;

namespace ConverterDxf.Tests.Geometry
{
    public class CurveTests : IClassFixture<ConverterFixture>
    {
        private readonly ConverterFixture fixture = new ConverterFixture();
    
        public static IEnumerable<object[]> LineData => ConverterSetup.GetTestMemberData<Line>();

        [Theory]
        [MemberData(nameof(LineData))]
        public void CanConvert_LineToNative(Line line)
        {
            var dxfLine = fixture.AssertAndConvertToNative<Dxfe.Line>(line);
            // TODO: Add line specific tests.
        }
    }
}