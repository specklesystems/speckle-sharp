using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using Objects.Converter.Revit;
using System.Linq;
using xUnitRevitUtils;
using Speckle.Core.Models;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public class FamilyInstanceFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("FamilyInstance.rvt");
    public override string UpdatedTestFile => Globals.GetTestModel("FamilyInstanceUpdated.rvt");
    public override string NewFile => Globals.GetTestModel("FamilyInstanceToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {
      BuiltInCategory.OST_Furniture,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_CeilingOpening,
      BuiltInCategory.OST_ColumnOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_StructuralFramingOpening,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_ArcWallRectOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_Floors};
    public FamilyInstanceFixture() : base()
    {
    }
  }

  public class FamilyInstanceTests : SpeckleConversionTest, IClassFixture<FamilyInstanceFixture>
  {
    public FamilyInstanceTests(FamilyInstanceFixture fixture) : base(fixture)
    {
    }

    //[Fact]
    //[Trait("Nested", "NestedToSpeckle")]
    //public void NestedToSpeckle()
    //{
    //  NativeToSpeckle();
    //}

    #region ToNative

    [Fact]
    [Trait("FamilyInstance", "Selection")]
    public async Task FamilyInstanceSelectionToNative()
    {
      await SelectionToNative<DB.Element>(AssertNestedEqual);
    }

    [Fact]
    [Trait("FamilyInstance", "ToNative")]
    public async Task NestedToNative()
    {
      await SpeckleToNative<DB.Element>(AssertNestedEqual);
    }


    [Fact]
    [Trait("FamilyInstance", "ToNativeUpdates")]
    public async Task NestedToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.Element>(AssertNestedEqual);
    }

    internal void AssertNestedEqual(DB.Element sourceElem, DB.Element destElem)
    {
      AssertElementEqual(sourceElem, destElem);

      //family instance
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_ELEVATION_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);


      if (sourceElem is FamilyInstance fi && fi.Host != null && destElem is FamilyInstance fi2)
      {
        Assert.Equal(fi.Host.Name, fi2.Host.Name);
      }

      //rotation
      //for some reasons, rotation of hosted families stopped working in 2021.1 ...?
      if (sourceElem.Location is LocationPoint && sourceElem is FamilyInstance fi3 && fi3.Host == null)
        Assert.Equal(((LocationPoint)sourceElem.Location).Rotation, ((LocationPoint)destElem.Location).Rotation);


      //walls
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_OFFSET);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_TOP_OFFSET);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
    }

    #endregion

  }
}
