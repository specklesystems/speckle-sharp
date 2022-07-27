using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using Objects.Geometry;
using Speckle.Core.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using xUnitRevitUtils;

namespace ConverterRevitTests
{
  public class BrepFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Brep.rvt");

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

    [Theory]
    [Trait("Brep", "ToNative")]
    [InlineData(@"Brep-Cube.json")]
    [InlineData(@"Brep-CubeWithHole.json")]
    [InlineData(@"Brep-TwoFaces.json")]
    [InlineData(@"Brep-TrimmedFace.json")]
    [InlineData(@"Brep-TrimmedFaceSingleLoop.json")]
    [InlineData(@"Brep-FaceWithHole.json")]
    [InlineData(@"Brep-NurbsWithHole.json")]
    [InlineData(@"Brep-TwoFacesWithHole.json")]
    [InlineData(@"Brep-Complex.json")]
    [InlineData(@"Brep-QuadDome.json")]
    [InlineData(@"Brep-SimpleHyparHole.json")]
    [InlineData(@"Brep-FaceWithTrimmedEdge.json")]
    [InlineData(@"Brep-Boat.json")]
    [InlineData(@"Brep-Plane.json")]
    public void BrepToNative(string fileName)
    {

      // Read and obtain `base` object.
      var contents = System.IO.File.ReadAllText(Globals.GetTestModel(fileName));
      var converter = new ConverterRevit();
      var @base = Operations.Deserialize(contents);

      // You read the wrong file, OOOPS!!
      if (!(@base is Brep brep)) throw new Exception("Object was not a brep, did you choose the right file?");
      DirectShape native = null;

      xru.RunInTransaction(() =>
      {
        converter.SetContextDocument(fixture.NewDoc);
        native = converter.BrepToDirectShape(brep, out List<string>notes);
      }, fixture.NewDoc).Wait();

      Assert.True(native.get_Geometry(new Options()).First() is Solid);
    }

    [Fact]
    [Trait("Brep", "ToSpeckle")]
    public void BrepToSpeckle()
    {
      throw new NotImplementedException();
    }

    [Fact]
    [Trait("Brep", "Selection")]
    public void BrepSelectionToNative()
    {
      var converter = new ConverterRevit();
      converter.SetContextDocument(fixture.NewDoc);

      if (!(fixture.Selection[0] is DirectShape ds))
        throw new Exception("Selected object was not a direct shape.");
      var geo = ds.get_Geometry(new Options());
      if (!(geo.First() is Solid solid))
        throw new Exception("DS was not composed of a solid.");
      var converted = converter.BrepToSpeckle(solid, fixture.NewDoc);
      var nativeconverted = converter.BrepToNative(converted, out List<string> notes);
      Assert.NotNull(nativeconverted);
    }

  }

}

