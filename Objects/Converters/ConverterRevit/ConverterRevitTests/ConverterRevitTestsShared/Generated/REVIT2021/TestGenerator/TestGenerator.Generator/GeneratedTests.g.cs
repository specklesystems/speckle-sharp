
using System.Threading.Tasks;
using Xunit;
using DB = Autodesk.Revit.DB;

namespace ConverterRevitTests
{

  public class AdaptiveComponentAdaptiveComponentFixture : SpeckleConversionFixture
  {
    public override string Category => "AdaptiveComponent";
    public override string TestName => "AdaptiveComponent";

    public AdaptiveComponentAdaptiveComponentFixture() : base()
    {
    }
  }

  public class AdaptiveComponentAdaptiveComponentTests : SpeckleConversionTest, IClassFixture<AdaptiveComponentAdaptiveComponentFixture>
  {
    public AdaptiveComponentAdaptiveComponentTests(AdaptiveComponentAdaptiveComponentFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("AdaptiveComponent", "AdaptiveComponentToNative")]
    public async Task AdaptiveComponentAdaptiveComponentToNative()
    {
      await SpeckleToNative();
    }

  }

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
    [Trait("Beam", "BeamToNativeUpdates")]
    public async Task BeamBeamToNativeUpdates()
    {
      await SpeckleToNativeUpdates();
    }

  }

  public class ColumnColumnFixture : SpeckleConversionFixture
  {
    public override string Category => "Column";
    public override string TestName => "Column";

    public ColumnColumnFixture() : base()
    {
    }
  }

  public class ColumnColumnTests : SpeckleConversionTest, IClassFixture<ColumnColumnFixture>
  {
    public ColumnColumnTests(ColumnColumnFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Column", "ColumnToNativeUpdates")]
    public async Task ColumnColumnToNativeUpdates()
    {
      await SpeckleToNativeUpdates();
    }

  }

  public class CurveCurveFixture : SpeckleConversionFixture
  {
    public override string Category => "Curve";
    public override string TestName => "Curve";

    public CurveCurveFixture() : base()
    {
    }
  }

  public class CurveCurveTests : SpeckleConversionTest, IClassFixture<CurveCurveFixture>
  {
    public CurveCurveTests(CurveCurveFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Curve", "CurveToNative")]
    public async Task CurveCurveToNative()
    {
      await SpeckleToNative();
    }

  }

  public class DirectShapeDirectShapeFixture : SpeckleConversionFixture
  {
    public override string Category => "DirectShape";
    public override string TestName => "DirectShape";

    public DirectShapeDirectShapeFixture() : base()
    {
    }
  }

  public class DirectShapeDirectShapeTests : SpeckleConversionTest, IClassFixture<DirectShapeDirectShapeFixture>
  {
    public DirectShapeDirectShapeTests(DirectShapeDirectShapeFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("DirectShape", "DirectShapeToNative")]
    public async Task DirectShapeDirectShapeToNative()
    {
      await SpeckleToNative();
    }

  }

  public class FamilyInstanceFamilyInstanceFixture : SpeckleConversionFixture
  {
    public override string Category => "FamilyInstance";
    public override string TestName => "FamilyInstance";

    public FamilyInstanceFamilyInstanceFixture() : base()
    {
    }
  }

  public class FamilyInstanceFamilyInstanceTests : SpeckleConversionTest, IClassFixture<FamilyInstanceFamilyInstanceFixture>
  {
    public FamilyInstanceFamilyInstanceTests(FamilyInstanceFamilyInstanceFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("FamilyInstance", "FamilyInstanceToNativeUpdates")]
    public async Task FamilyInstanceFamilyInstanceToNativeUpdates()
    {
      await SpeckleToNativeUpdates();
    }

  }

  public class FloorFloorFixture : SpeckleConversionFixture
  {
    public override string Category => "Floor";
    public override string TestName => "Floor";

    public FloorFloorFixture() : base()
    {
    }
  }

  public class FloorFloorTests : SpeckleConversionTest, IClassFixture<FloorFloorFixture>
  {
    public FloorFloorTests(FloorFloorFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Floor", "FloorToNative")]
    public async Task FloorFloorToNative()
    {
      await SpeckleToNative();
    }

  }

  public class OpeningOpeningFixture : SpeckleConversionFixture
  {
    public override string Category => "Opening";
    public override string TestName => "Opening";

    public OpeningOpeningFixture() : base()
    {
    }
  }

  public class OpeningOpeningTests : SpeckleConversionTest, IClassFixture<OpeningOpeningFixture>
  {
    public OpeningOpeningTests(OpeningOpeningFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Opening", "OpeningToNative")]
    public async Task OpeningOpeningToNative()
    {
      await SpeckleToNative();
    }

  }

  public class RoofRoofFixture : SpeckleConversionFixture
  {
    public override string Category => "Roof";
    public override string TestName => "Roof";

    public RoofRoofFixture() : base()
    {
    }
  }

  public class RoofRoofTests : SpeckleConversionTest, IClassFixture<RoofRoofFixture>
  {
    public RoofRoofTests(RoofRoofFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Roof", "RoofToNative")]
    public async Task RoofRoofToNative()
    {
      await SpeckleToNative();
    }

  }

  public class RoomRoomFixture : SpeckleConversionFixture
  {
    public override string Category => "Room";
    public override string TestName => "Room";

    public RoomRoomFixture() : base()
    {
    }
  }

  public class RoomRoomTests : SpeckleConversionTest, IClassFixture<RoomRoomFixture>
  {
    public RoomRoomTests(RoomRoomFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Room", "RoomToSpeckle")]
    public async Task RoomRoomToSpeckle()
    {
      await NativeToSpeckle();
    }

  }

  public class ScheduleScheduleFixture : SpeckleConversionFixture
  {
    public override string Category => "Schedule";
    public override string TestName => "Schedule";

    public ScheduleScheduleFixture() : base()
    {
    }
  }

  public class ScheduleScheduleTests : SpeckleConversionTest, IClassFixture<ScheduleScheduleFixture>
  {
    public ScheduleScheduleTests(ScheduleScheduleFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Schedule", "ScheduleToNativeUpdates")]
    public async Task ScheduleScheduleToNativeUpdates()
    {
      await SpeckleToNativeUpdates();
    }

  }

  public class WallWallFixture : SpeckleConversionFixture
  {
    public override string Category => "Wall";
    public override string TestName => "Wall";

    public WallWallFixture() : base()
    {
    }
  }

  public class WallWallTests : SpeckleConversionTest, IClassFixture<WallWallFixture>
  {
    public WallWallTests(WallWallFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Wall", "WallToNativeUpdates")]
    public async Task WallWallToNativeUpdates()
    {
      await SpeckleToNativeUpdates();
    }

  }

  public class WireWireFixture : SpeckleConversionFixture
  {
    public override string Category => "Wire";
    public override string TestName => "Wire";

    public WireWireFixture() : base()
    {
    }
  }

  public class WireWireTests : SpeckleConversionTest, IClassFixture<WireWireFixture>
  {
    public WireWireTests(WireWireFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Wire", "WireToNative")]
    public async Task WireWireToNative()
    {
      await SpeckleToNative();
    }

  }

}
