using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public class RoofFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Roof.rvt");
    public override string NewFile => Globals.GetTestModel("RoofToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Roofs };

    public RoofFixture() : base()
    {
    }
  }

  public class RoofTests : SpeckleConversionTest, IClassFixture<RoofFixture>
  {
    public RoofTests(RoofFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Roof", "ToSpeckle")]
    public async Task RoofToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Roof", "ToNative")]
    public async Task RoofToNative()
    {
      await SpeckleToNative<DB.RoofBase>(AssertRoofEqual);
    }

    [Fact]
    [Trait("Roof", "Selection")]
    public async Task RoofSelectionToNative()
    {
      await SelectionToNative<DB.RoofBase>(AssertRoofEqual);
    }

    private void AssertRoofEqual(DB.RoofBase sourceElem, DB.RoofBase destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.ROOF_SLOPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.ROOF_BASE_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.ROOF_UPTO_LEVEL_PARAM);
    }

    #endregion ToNative
  }
}
