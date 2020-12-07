using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;


namespace ConverterRevitTests
{
  public class BeamFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("FamilyInstance.rvt");

    public override string UpdatedTestFile => Globals.GetTestModel("FamilyInstanceUpdated.rvt");
    public override string NewFile => Globals.GetTestModel("FamilyInstance_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming };
    public BeamFixture() : base()
    {
    }
  }

  public class BeamTests : SpeckleConversionTest, IClassFixture<BeamFixture>
  {
    public BeamTests(BeamFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("Beam", "ToSpeckle")]
    public void BeamToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Beam", "ToNative")]
    public void BeamToNative()
    {
      SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Beam", "ToNativeUpdates")]
    public void WallToNativeUpdates()
    {
      SpeckleToNativeUpdates<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Beam", "Selection")]
    public void BeamSelectionToNative()
    {
      SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    #endregion

  }
}
