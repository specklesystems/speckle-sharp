
using System.Threading.Tasks;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{

  public class BeamBeamFixture : SpeckleConversionFixture
  {
    public override string Category => "Beam";
    public override string TestName => "Beam";

    public BeamBeamFixture() : base()
    {
    }
  }

  public class BeamBeamTests : SpeckleConversionTest, IClassFixture<BeamBeamFixture>
  {
    public BeamBeamTests(BeamBeamFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Beam", "BeamToSpeckle")]
    public async Task BeamBeamToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Beam", "BeamToNative")]
    public async Task BeamBeamToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Beam", "BeamToNativeUpdates")]
    public async Task BeamBeamToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }

    [Fact]
    [Trait("Beam", "BeamSelection")]
    public async Task BeamBeamSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual);
    }
  }

}
