using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Objects;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Wall = Objects.Wall;
using Element = Objects.Element;
using xUnitRevitUtils;
using Autodesk.Revit.UI;

namespace ConverterRevitTests
{
  public class OpeningFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Opening.rvt");
    public override string NewFile => Globals.GetTestModel("Opening_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> {
      BuiltInCategory.OST_CeilingOpening,
      BuiltInCategory.OST_ColumnOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_StructuralFramingOpening,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_ArcWallRectOpening,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_RoofOpening,
      BuiltInCategory.OST_Roofs};
    public OpeningFixture() : base()
    {
    }
  }

  public class OpeningTests : SpeckleConversionTest, IClassFixture<OpeningFixture>
  {
    public OpeningTests(OpeningFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("Opening", "ToSpeckle")]
    public void OpeningToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Opening", "ToNative")]
    public void OpeningToNative()
    {
      SpeckleToNative<DB.Opening>(AssertOpeningEqual);
    }

    [Fact]
    [Trait("Opening", "Selection")]
    public void OpeningSelectionToNative()
    {
      SelectionToNative<DB.Opening>(AssertOpeningEqual);
    }

    private void AssertOpeningEqual(DB.Opening sourceElem, DB.Opening destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, sourceElem.Name);

      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_BASE_CONSTRAINT);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_HEIGHT_TYPE);
      AssertEqualParam(sourceElem, destElem, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
    }


    #endregion

  }
}
