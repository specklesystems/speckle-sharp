using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public class CurveFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Curve.rvt");
    public override string NewFile => Globals.GetTestModel("CurveToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Lines, BuiltInCategory.OST_RoomSeparationLines };
    public CurveFixture() : base()
    {
    }
  }

  public class CurveTests : SpeckleConversionTest, IClassFixture<CurveFixture>
  {
    public CurveTests(CurveFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Curve", "ToSpeckle")]
    public async Task CurveToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Curve", "ToNative")]
    public async Task CurveToNative()
    {
      await SpeckleToNative<DB.CurveElement>(AssertCurveEqual);
    }

    [Fact]
    [Trait("Curve", "Selection")]
    public async Task CurveSelectionToNative()
    {
      await SelectionToNative<DB.CurveElement>(AssertCurveEqual);
    }

    internal void AssertCurveEqual(DB.CurveElement sourceElem, DB.CurveElement destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.BUILDING_CURVE_GSTYLE);

      if (((LocationCurve)sourceElem.Location).Curve.IsBound){
        var sourceEnd = ((LocationCurve)sourceElem.Location).Curve.GetEndPoint(0);
        var destEnd = ((LocationCurve)destElem.Location).Curve.GetEndPoint(0);

        Assert.Equal(sourceEnd.X, destEnd.X, 4);
        Assert.Equal(sourceEnd.Y, destEnd.Y, 4);
        Assert.Equal(sourceEnd.Z, destEnd.Z, 4);
      }
    }

    #endregion

  }
}
