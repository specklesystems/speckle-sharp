using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public class AdaptiveComponentFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("AdaptiveComponent.rvt");
    public override string NewFile => Globals.GetTestModel("AdaptiveComponentToNative.rvt");
    //USING GENERIC MODELS FOR AC, fine for testing
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_GenericModel };
    public AdaptiveComponentFixture() : base ()
    {
    }
  }

  public class AdaptiveComponentTests : SpeckleConversionTest, IClassFixture<AdaptiveComponentFixture>
  {
    public AdaptiveComponentTests(AdaptiveComponentFixture fixture) : base (fixture)
    { 
    }

    [Fact]
    [Trait("AdaptiveComponent", "ToSpeckle")]
    public async Task AdaptiveComponentToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("AdaptiveComponent", "ToNative")]
    public async Task AdaptiveComponentToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AdaptiveComponentEqual);
    }

    [Fact]
    [Trait("AdaptiveComponent", "Selection")]
    public async Task AdaptiveComponentSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AdaptiveComponentEqual);
    }

    private void AdaptiveComponentEqual(DB.FamilyInstance sourceElem, DB.FamilyInstance destElem)
    {
      AssertElementEqual(sourceElem, destElem);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FLEXIBLE_INSTANCE_FLIP);

      var dist = (sourceElem.Location as LocationPoint).Point.DistanceTo((destElem.Location as LocationPoint).Point);

      Assert.True(dist<0.1);
    }

    #endregion

  }
}
