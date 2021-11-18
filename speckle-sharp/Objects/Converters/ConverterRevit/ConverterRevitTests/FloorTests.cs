using Autodesk.Revit.DB;
using System.Collections.Generic;
using Xunit;

using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public class FloorFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Floor.rvt");
    public override string NewFile => Globals.GetTestModel("Floor_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Floors };

    public FloorFixture() : base()
    {
    }
  }

  public class FloorTests : SpeckleConversionTest, IClassFixture<FloorFixture>
  {
    public FloorTests(FloorFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("Floor", "ToSpeckle")]
    public void FloorToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Floor", "ToNative")]
    public void FloorToNative()
    {
      SpeckleToNative<DB.Floor>(AssertFloorEqual);
    }

    [Fact]
    [Trait("Floor", "Selection")]
    public void FloorSelectionToNative()
    {
      SelectionToNative<DB.Floor>(AssertFloorEqual);
    }

    private void AssertFloorEqual(DB.Floor sourceElem, DB.Floor destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.HOST_PERIMETER_COMPUTED);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
    }

    #endregion ToNative
  }
}