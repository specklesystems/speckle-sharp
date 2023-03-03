﻿using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConverterRevitTests
{
  public class DuctFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Duct.rvt");
    public override string NewFile => Globals.GetTestModel("Duct_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_DuctCurves };
    public DuctFixture() : base() { }
  }
  public class DuctTests : SpeckleConversionTest, IClassFixture<DuctFixture>
  {
    public DuctTests(DuctFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("Duct", "ToSpeckle")]
    public void DuctToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Duct", "ToNative")]
    public void DuctToNative()
    {
      SpeckleToNative<DB.Duct>(AssertDuctEqual);
    }

    private void AssertDuctEqual(DB.Duct sourceElem, DB.Duct destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.CURVE_ELEM_LENGTH);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_START_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_CURVE_WIDTH_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.RBS_VELOCITY);
    }
    #endregion

  }
}
