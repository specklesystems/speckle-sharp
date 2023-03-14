using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{
  public class ColumnFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Column.rvt");
    public override string UpdatedTestFile => Globals.GetTestModel("ColumnUpdated.rvt");
    public override string NewFile => Globals.GetTestModel("ColumnToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns };

    public ColumnFixture() : base()
    {
    }
  }

  public class ColumnTests : SpeckleConversionTest, IClassFixture<ColumnFixture>
  {
    public ColumnTests(ColumnFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Column", "ToSpeckle")]
    public async Task ColumnToSpeckle()
    {
      await NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("Column", "ToNative")]
    public async Task ColumnToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Column", "ToNativeUpdates")]
    public async Task ColumnToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Column", "Selection")]
    public async Task ColumnSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    #endregion ToNative
  }
}
