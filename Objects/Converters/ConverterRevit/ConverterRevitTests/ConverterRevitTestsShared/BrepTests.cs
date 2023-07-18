using Autodesk.Revit.DB;
using Autofac;
using ConnectorRevit.Operations;
using ConnectorRevit.Services;
using ConverterRevitTestsShared.Services;
using Objects.Converter.Revit;
using Objects.Geometry;
using RevitSharedResources.Interfaces;
using Speckle.Core.Api;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ConverterRevitTests
{
  public class BrepFixture : SpeckleConversionFixture
  {
    public override string TestName => "Brep";
    public override string Category => TestCategories.Brep;
  }

  public class BrepTests : SpeckleConversionTest, IClassFixture<BrepFixture>
  {
    public BrepTests(BrepFixture fixture) : base(fixture)
    {
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
      if (!(@base is Brep brep)) throw new Exception("Object was not a brep, did you choose the right file?");
      brep.applicationId ??= "dummyAppId";

      var scope = CreateScope(fixture.NewDoc);
      var setReceiveObjectFunc = scope.Resolve<Func<Base, ISpeckleObjectReceiver>>();
      setReceiveObjectFunc(brep);

      var receiveOp = scope.Resolve<ReceiveOperation>();

      await receiveOp.Receive().ConfigureAwait(false);
      var convertedObjects = scope.Resolve<IConvertedObjectsCache<Base, Element>>();

      Assert.Single(convertedObjects.GetCreatedObjects());
      var native = convertedObjects.GetCreatedObjects().First() as DirectShape;
      Assert.True(native.get_Geometry(new Options()).First() is Solid);
    }

    [Fact]
    [Trait("Brep", "ToSpeckle")]
    public async Task BrepToSpeckle()
    {
      await NativeToSpeckle();
    }

    public override void OverrideDependencies(ContainerBuilder builder)
    {
      builder.RegisterType<SpecificObjectReciever>().As<ISpeckleObjectReceiver>()
        .InstancePerLifetimeScope();
    }
  }
}

