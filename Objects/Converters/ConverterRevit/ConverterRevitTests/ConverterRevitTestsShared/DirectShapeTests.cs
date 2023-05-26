//using DB = Autodesk.Revit.DB;
//using Xunit;
//using System.Threading.Tasks;

//namespace ConverterRevitTests
//{
//  public class DirectShapeFixture : SpeckleConversionFixture
//  {
//    public override string TestFile => Globals.GetTestModelOfCategory(Category, "DirectShape.rvt");
//    public override string NewFile => Globals.GetTestModelOfCategory(Category, "DirectShapeToNative.rvt");
//    public override string Category => TestCategories.DirectShape;
//    public DirectShapeFixture() : base ()
//    {
//    }
//  }

//  public class DirectShapeTests : SpeckleConversionTest, IClassFixture<DirectShapeFixture>
//  {
//    public DirectShapeTests(DirectShapeFixture fixture) : base(fixture)
//    {
//    }

//    [Fact]
//    [Trait("DirectShape", "ToSpeckle")]
//    public async Task DirectShapeToSpeckle()
//    {
//      await NativeToSpeckle();
//    }

//    #region ToNative

//    [Fact]
//    [Trait("DirectShape", "ToNative")]
//    public async Task DirectShapeToNative()
//    {
//      await SpeckleToNative<DB.DirectShape>(DirectShapeEqual);
//    }

//    [Fact]
//    [Trait("DirectShape", "Selection")]
//    public async Task DirectShapeSelectionToNative()
//    {
//      await SelectionToNative<DB.DirectShape>(DirectShapeEqual);
//    }

//    private void DirectShapeEqual(DB.DirectShape sourceElem, DB.DirectShape destElem)
//    {
//      AssertElementEqual(sourceElem, destElem);
//    }

//    #endregion

//  }
//}
