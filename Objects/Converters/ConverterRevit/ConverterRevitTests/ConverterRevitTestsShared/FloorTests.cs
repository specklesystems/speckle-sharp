using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using Revit.Async;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public class FloorFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Floor.rvt");
    public override string NewFile => Globals.GetTestModel("FloorToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Floors };

    public FloorFixture() : base()
    {
    }
  }

  public class FloorTests : SpeckleConversionTest, IClassFixture<FloorFixture>
  {
    public FloorTests(FloorFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Floor", "ToSpeckle")]
    public async Task FloorToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Floor", "ToNative")]
    public async Task FloorToNative()
    {
      await SpeckleToNative<DB.Floor>(null, AssertFloorEqual);
    }

    [Fact]
    [Trait("Floor", "Selection")]
    public async Task FloorSelectionToNative()
    {
      await SelectionToNative<DB.Floor>(null,AssertFloorEqual);
    }

    private async Task AssertFloorEqual(DB.Floor sourceElem, DB.Floor destElem)
    {
      AssertElementEqual(sourceElem, destElem);

      var slopeArrow = await RevitTask.RunAsync(app => {
        return ConverterRevit.GetSlopeArrow(sourceElem);
      }).ConfigureAwait(false);

      if (slopeArrow == null)
      {
        AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
      }
      else
      {
        var tailOffset = ConverterRevit.GetSlopeArrowTailOffset(slopeArrow, sourceElem.Document);
        _ = ConverterRevit.GetSlopeArrowHeadOffset(slopeArrow, sourceElem.Document, tailOffset, out var slope);

        var newSlopeArrow = await RevitTask.RunAsync(app => {
          return ConverterRevit.GetSlopeArrow(destElem);
        }).ConfigureAwait(false);

        Assert.NotNull(newSlopeArrow);

        var newTailOffset = ConverterRevit.GetSlopeArrowTailOffset(slopeArrow, sourceElem.Document);
        _ = ConverterRevit.GetSlopeArrowHeadOffset(slopeArrow, sourceElem.Document, tailOffset, out var newSlope);
        Assert.Equal(slope, newSlope);

        var sourceOffset = ConverterRevit.GetParamValue<double>(sourceElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
        Assert.Equal(sourceOffset + tailOffset, ConverterRevit.GetParamValue<double>(destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM));
      }

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
    }

    #endregion ToNative
  }
}
