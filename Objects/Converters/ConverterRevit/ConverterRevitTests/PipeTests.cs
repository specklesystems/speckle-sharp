using System.Collections.Generic;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB.Plumbing;
using Xunit;

namespace ConverterRevitTests
{
  public class PipeFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Pipe.rvt");
    public override string NewFile => Globals.GetTestModel("Pipe_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {BuiltInCategory.OST_PipeCurves};
  }

  public class PipeTests : SpeckleConversionTest, IClassFixture<PipeFixture>
  {
    public PipeTests(PipeFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("Pipe", "ToSpeckle")]
    public void PipeToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Pipe", "ToNative")]
    public void PipeToNative()
    {
      SpeckleToNative<DB.Pipe>(AssertPipeEqual);
    }

    private void AssertPipeEqual(DB.Pipe sourceElem, DB.Pipe destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_START_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
    }

    #endregion ToNative
  }
}