using System.Collections.Generic;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB.Electrical;
using Xunit;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public class WireFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Wire.rvt");
    public override string NewFile => Globals.GetTestModel("WireToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {BuiltInCategory.OST_Wire};
  }

  public class WireTests : SpeckleConversionTest, IClassFixture<WireFixture>
  {
    public WireTests(WireFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Wire", "ToSpeckle")]
    public async Task WireToSpeckle()
    {
      await NativeToSpeckle();
    }


    #region ToNative

    [Fact]
    [Trait("Wire", "ToNative")]
    public async Task WireToNative()
    {
      await SpeckleToNative<DB.Wire>(AssertWireEqual);
    }

    private void AssertWireEqual(DB.Wire sourceElem, DB.Wire destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_ELEC_WIRE_TYPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FABRIC_WIRE_LENGTH);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_ELEC_WIRE_ELEVATION);
    }

    #endregion ToNative
  }
}
