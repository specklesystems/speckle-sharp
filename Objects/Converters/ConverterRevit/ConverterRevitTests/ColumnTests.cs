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
  public class ColumnFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("FamilyInstance.rvt");
    public override string NewFile => Globals.GetTestModel("FamilyInstance_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns };
    public ColumnFixture() : base ()
    {
    }
  }

  public class ColumnTests : SpeckleConversionTest, IClassFixture<ColumnFixture>
  {
    public ColumnTests(ColumnFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("Column", "ToSpeckle")]
    public void ColumnToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Column", "ToNative")]
    public void ColumnToNative()
    {
      SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Column", "Selection")]
    public void ColumnSelectionToNative()
    {
      SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }




    #endregion

  }
}
