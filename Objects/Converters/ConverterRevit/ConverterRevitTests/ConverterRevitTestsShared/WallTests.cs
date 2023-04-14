using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConverterRevitTests
{

  public class WallFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Wall.rvt");
    public override string UpdatedTestFile => Globals.GetTestModel("WallUpdated.rvt");
    public override string NewFile => Globals.GetTestModel("WallToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Walls };
    public WallFixture() : base()
    {
    }
  }
  public class WallTests : SpeckleConversionTest, IClassFixture<WallFixture>
  {

    public WallTests(WallFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Wall", "ToSpeckle")]
    public async Task WallToSpeckle()
    {
      await NativeToSpeckle();
    }


    [Fact]
    [Trait("Wall", "ToNative")]
    public async Task WallToNative()
    {
      await SpeckleToNative<DB.Wall>(AssertWallEqual);
    }

    [Fact]
    [Trait("Wall", "ToNativeUpdates")]
    public async Task WallToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.Wall>(AssertWallEqual);
    }


    [Fact]
    [Trait("Wall", "Selection")]
    public async Task WallSelectionToNative()
    {
      await SelectionToNative<DB.Wall>(AssertWallEqual);
    }

    private void AssertWallEqual(DB.Wall sourceElem, DB.Wall destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);


      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_OFFSET);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_TOP_OFFSET);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
    }
  }
}
