using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using Objects.Converter.Revit;
using System.Linq;
using xUnitRevitUtils;
using Speckle.Core.Models;

namespace ConverterRevitTests
{
  public class NestedFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Nested.rvt");
    public override string NewFile => Globals.GetTestModel("Nested_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {
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
    public NestedFixture() : base()
    {
    }
  }

  public class NestedTests : SpeckleConversionTest, IClassFixture<NestedFixture>
  {
    public NestedTests(NestedFixture fixture)
    {
      this.fixture = fixture;
    }

    //[Fact]
    //[Trait("Nested", "NestedToSpeckle")]
    //public void NestedToSpeckle()
    //{
    //  NativeToSpeckle();
    //}

    #region ToNative

    [Fact]
    [Trait("Nested", "ToNative")]
    public void NestedToNative()
    {
      ConverterRevit kit = new ConverterRevit();
      var spkElems = kit.ConvertToSpeckle(fixture.RevitElements.Select(x => (object)x).ToList());

      kit = new ConverterRevit();
      var revitEls = new List<DB.Element>();

      xru.RunInTransaction(() =>
      {
        revitEls = kit.ConvertToNative(spkElems).Select(x => (DB.Element)x).ToList();
      }, fixture.NewDoc).Wait();

      Assert.Empty(kit.ConversionErrors);

      //for (var i = 0; i < revitEls.Count; i++)
      //{
      //  var sourceElem = fixture.RevitElements[i];
      //  var destElem = revitEls[i];
      //  AssertNestedEqual(sourceElem, destElem);
      //}
    }

    //[Fact]
    //[Trait("Nested", "NestedSelection")]
    //public void NestedSelectionToNative()
    //{
    //  SelectionToNative<DB.Element>(AssertNestedEqual);
    //}

    internal void AssertNestedEqual(DB.Element sourceElem, DB.Element destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);

      //family instance
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
      //AssertEqualParam(sourceElem, destElem, BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

      //rotation
      if (sourceElem.Location is LocationPoint)
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
