using Autodesk.Revit.DB;
using Objects.Converter.Revit;
using Objects.Geometry;
using Speckle.Core.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ConverterRevitTests;

public class BrepFixture : SpeckleConversionFixture
{
  public override string TestName => "Brep";
  public override string Category => TestCategories.Brep;
}

public class BrepTests : SpeckleConversionTest, IClassFixture<BrepFixture>
{
  private readonly ITestOutputHelper _testOutputHelper;

  public BrepTests(BrepFixture fixture, ITestOutputHelper testOutputHelper)
    : base(fixture)
  {
    _testOutputHelper = testOutputHelper;
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
  public async Task BrepToNative(string fileName)
  {
    // Read and obtain `base` object.
    var contents = System.IO.File.ReadAllText(Globals.GetTestModelOfCategory(fixture.Category, fileName));
    var converter = new ConverterRevit();
    var @base = Operations.Deserialize(contents);

    // You read the wrong file, OOOPS!!
    if (!(@base is Brep brep))
    {
      throw new Exception("Object was not a brep, did you choose the right file?");
    }

    DirectShape native = null;

    await SpeckleUtils.RunInTransaction(
      () =>
      {
        converter.SetContextDocument(fixture.NewDoc);
        native = converter.BrepToDirectShape(brep, out List<string> notes);
      },
      fixture.NewDoc,
      converter
    );

    Assert.True(native.get_Geometry(new Options()).First() is Solid);
  }

  [Fact]
  [Trait("Brep", "ToSpeckle")]
  public async Task BrepToSpeckle()
  {
    await NativeToSpeckle();
  }

  [Fact]
  [Trait("Brep", "Selection")]
  public void BrepSelectionToNative()
  {
    var converter = new ConverterRevit();
    converter.SetContextDocument(fixture.NewDoc);

    if (fixture.Selection.Count == 0)
    {
      return;
    }

    if (!(fixture.Selection[0] is DirectShape ds))
    {
      throw new Exception("Selected object was not a direct shape.");
    }

    var geo = ds.get_Geometry(new Options());
    if (!(geo.First() is Solid solid))
    {
      throw new Exception("DS was not composed of a solid.");
    }

    var converted = converter.BrepToSpeckle(solid, fixture.NewDoc);
    var nativeconverted = converter.BrepToNative(converted, out List<string> notes);
    Assert.NotNull(nativeconverted);
  }
}
