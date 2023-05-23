using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB.Plumbing;
using Xunit;

namespace ConverterRevitTests
{
  public class PipeFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModelOfCategory(Category, "Pipe.rvt");
    public override string NewFile => Globals.GetTestModelOfCategory(Category, "PipeToNative.rvt");
    public override string Category => TestCategories.Pipe;
  }

  public class PipeTests : SpeckleConversionTest, IClassFixture<PipeFixture>
  {
    public PipeTests(PipeFixture fixture) : base(fixture)
    {
    }

    //[Fact]
    //[Trait("Pipe", "ToSpeckle")]
    //public async Task PipeToSpeckle()
    //{
    //  await NativeToSpeckle();
    //}

    //[Fact]
    //[Trait("Pipe", "ToNative")]
    //public async Task PipeToNative()
    //{
    //  await SpeckleToNative<DB.Pipe>(AssertPipeEqual);
    //}

    private void AssertPipeEqual(DB.Pipe sourceElem, DB.Pipe destElem)
    {
      AssertElementEqual(sourceElem, destElem);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_START_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
    }
  }
}
