using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;


namespace ConverterRevitTests
{
  public class DirectShapeFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("DirectShape.rvt");
    public override string NewFile => Globals.GetTestModel("DirectShape_ToNative.rvt");
    //USING GENERIC MODELS FOR AC, fine for testing
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_GenericModel };
    public DirectShapeFixture() : base ()
    {
    }
  }

  public class DirectShapeTests : SpeckleConversionTest, IClassFixture<DirectShapeFixture>
  {
    public DirectShapeTests(DirectShapeFixture fixture)
    {
      this.fixture = fixture;
    }

    [Fact]
    [Trait("DirectShape", "ToSpeckle")]
    public void DirectShapeToSpeckle()
    {
      NativeToSpeckle();
    }

    #region ToNative

    [Fact]
    [Trait("DirectShape", "ToNative")]
    public void DirectShapeToNative()
    {
      SpeckleToNative<DB.DirectShape>(DirectShapeEqual);
    }

    [Fact]
    [Trait("DirectShape", "Selection")]
    public void DirectShapeSelectionToNative()
    {
      SelectionToNative<DB.DirectShape>(DirectShapeEqual);
    }

    private void DirectShapeEqual(DB.DirectShape sourceElem, DB.DirectShape destElem)
    {
      Assert.NotNull(destElem);
      Assert.Equal(sourceElem.Name, destElem.Name);
      Assert.Equal(sourceElem.Category.Name, destElem.Category.Name);

    }

    #endregion

  }
}
