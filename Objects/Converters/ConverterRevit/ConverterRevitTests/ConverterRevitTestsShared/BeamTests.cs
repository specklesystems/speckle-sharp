using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public class BeamFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModelOfCategory(Category, "Beam.rvt");

    public override string UpdatedTestFile => Globals.GetTestModelOfCategory(Category, "BeamUpdated.rvt");
    public override string NewFile => Globals.GetTestModelOfCategory(Category, "BeamToNative.rvt");
    public override string ExpectedFailuresFile => Globals.GetTestModelOfCategory(Category, "Beam.ExpectedFailures.json");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming };

    public override string TestName => "Beam";

    public override string Category => "Beam";

    public BeamFixture() : base()
    {
    }
  }

  public class BeamTests : SpeckleConversionTest, IClassFixture<BeamFixture>
  {
    public BeamTests(BeamFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Beam", "ToSpeckle")]
    public async Task BeamToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Beam", "ToNative")]
    public async Task BeamToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Beam", "ToNativeUpdates")]
    public async Task BeamToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Beam", "Selection")]
    public async Task BeamSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    #endregion

  }
}
