using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using Xunit;
using Xunit.Abstractions;
using Objects.Geometry;
using Speckle.Core.Api;
using xUnitRevitUtils;

namespace ConverterRevitTests
{
  public class BrepFixture: SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Curve.rvt");

    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Mass, BuiltInCategory.OST_Mass };

    public override string NewFile => Globals.GetTestModel("Brep_ToNative.rvt");
  }
  
  public class BrepTests : SpeckleConversionTest, IClassFixture<BrepFixture>
  {
    private readonly ITestOutputHelper _testOutputHelper;

    public BrepTests(BrepFixture fixture, ITestOutputHelper testOutputHelper)
    {
      _testOutputHelper = testOutputHelper;
      this.fixture = fixture;
    }
    public static string TestFolder => @"Y:\Documents\Speckle\speckle-sharp\Objects\Converters\ConverterRevit\TestModels\";
    [Theory]
    [Trait("Brep", "ToNative")]
    [InlineData(@"Brep-UnitCube.json")]
    [InlineData(@"Brep-TwoFaces.json")]
    [InlineData(@"Brep-TrimmedFace.json")]
    [InlineData(@"Brep-FaceWithHole.json")]
    public void BrepToNative(string fileName)
    {
      // Read and obtain `base` object.
      var contents = System.IO.File.ReadAllText(TestFolder + fileName);
      var converter = new ConverterRevit();
      var @base = Operations.Deserialize(contents);
      
      // You read the wrong file, OOOPS!!
      if (!(@base is Brep brep)) throw new Exception("Object was not a brep, did you choose the right file?");
      Solid native = null;
      
      xru.RunInTransaction(() =>
      {
        native = converter.BrepToNative(brep);
        var ds = DirectShape.CreateElement(fixture.NewDoc, new ElementId(BuiltInCategory.OST_GenericModel));
        ds.SetShape(new List<GeometryObject>{native});
      }, fixture.NewDoc ).Wait();
      
      Assert.NotNull(native);
    }

    [Fact]
    [Trait("Brep", "ToSpeckle")]
    public void BrepToSpeckle()
    {
      throw new NotImplementedException();
    }
  }

}

