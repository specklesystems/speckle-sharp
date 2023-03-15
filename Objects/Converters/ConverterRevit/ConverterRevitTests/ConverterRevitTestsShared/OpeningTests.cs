using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public class OpeningFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Opening.rvt");
    public override string NewFile => Globals.GetTestModel("OpeningToNative.rvt");

    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {
      BuiltInCategory.OST_CeilingOpening,
      BuiltInCategory.OST_ColumnOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_StructuralFramingOpening,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_ArcWallRectOpening,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_RoofOpening,
      BuiltInCategory.OST_Roofs};

    public OpeningFixture() : base()
    {
    }
  }

  public class OpeningTests : SpeckleConversionTest, IClassFixture<OpeningFixture>
  {
    public OpeningTests(OpeningFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Opening", "ToSpeckle")]
    public async Task OpeningToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Opening", "ToNative")]
    public async Task OpeningToNative()
    {
      await SpeckleToNative<DB.Element>(AssertOpeningEqual);
    }

    [Fact]
    [Trait("Opening", "Selection")]
    public async Task OpeningSelectionToNative()
    {
      await SelectionToNative<DB.Element>(AssertOpeningEqual);
    }

    private void AssertOpeningEqual(DB.Element sourceElem, DB.Element destElem)
    {
      if (!(sourceElem is DB.Opening))
        return;

      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
    }

    #endregion ToNative
  }
}
