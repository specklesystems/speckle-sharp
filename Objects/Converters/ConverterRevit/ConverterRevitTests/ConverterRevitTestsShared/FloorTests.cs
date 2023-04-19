using Autodesk.Revit.DB;
using Objects.Converter.Revit;
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
      await SpeckleToNative<DB.Floor>(AssertFloorEqual);
    }

    [Fact]
    [Trait("Floor", "Selection")]
    public async Task FloorSelectionToNative()
    {
      await SelectionToNative<DB.Floor>(AssertFloorEqual);
    }

    private void AssertFloorEqual(DB.Floor sourceElem, DB.Floor destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.)
      //var slopeArrow = ConverterRevit.GetSlopeArrowHack(sourceElem.Id, sourceElem.Document);

      //if (slopeArrow == null)
      //{
      //  AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
      //}
      //else
      //{
      //  var tailOffset = ConverterRevit.GetSlopeArrowTailOffset(slopeArrow, sourceElem.Document);
      //  var headOffset = ConverterRevit.GetSlopeArrowHeadOffset(slopeArrow, sourceElem.Document, tailOffset, out var slope);

      //  Assert.Equal(slope, ConverterRevit.GetParamValue<double>(destElem, BuiltInParameter.ROOF_SLOPE) / 100);

      //  var sourceOffset = ConverterRevit.GetParamValue<double>(sourceElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
      //  Assert.Equal(sourceOffset + tailOffset, ConverterRevit.GetParamValue<double>(destElem, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM));
      //}
      

      
      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.HOST_PERIMETER_COMPUTED);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM);
    }

    #endregion ToNative
  }
}
