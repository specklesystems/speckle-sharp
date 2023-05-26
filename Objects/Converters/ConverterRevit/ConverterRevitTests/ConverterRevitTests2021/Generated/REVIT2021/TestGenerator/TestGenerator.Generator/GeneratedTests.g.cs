
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
    [Trait("AdaptiveComponent", "AdaptiveComponentToSpeckle")]
    public async Task AdaptiveComponentAdaptiveComponentToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("AdaptiveComponent", "AdaptiveComponentToNative")]
    public async Task AdaptiveComponentAdaptiveComponentToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AdaptiveComponentEqual, );
    }

    [Fact]
    [Trait("AdaptiveComponent", "AdaptiveComponentSelection")]
    public async Task AdaptiveComponentAdaptiveComponentSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AdaptiveComponentEqual, );
    }

  }

  public class BeamBeam.ExpectedFailures.jsonFixture : SpeckleConversionFixture
  {
    public override string Category => "Beam";
    public override string TestName => "Beam.ExpectedFailures.json";

    public BeamBeam.ExpectedFailures.jsonFixture() : base()
    {
    }
  }

  public class BeamBeam.ExpectedFailures.jsonTests : SpeckleConversionTest, IClassFixture<BeamBeam.ExpectedFailures.jsonFixture>
  {
    public BeamBeam.ExpectedFailures.jsonTests(BeamBeam.ExpectedFailures.jsonFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Beam", "Beam.ExpectedFailures.jsonToSpeckle")]
    public async Task BeamBeam.ExpectedFailures.jsonToSpeckle()
    {
      await NativeToSpeckle();
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
    [Trait("Beam", "BeamToSpeckle")]
    public async Task BeamBeamToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Beam", "BeamToNative")]
    public async Task BeamBeamToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual, );
    }

    [Fact]
    [Trait("Beam", "BeamSelection")]
    public async Task BeamBeamSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual, );
    }

    [Fact]
    [Trait("Beam", "BeamToNativeUpdates")]
    public async Task BeamBeamToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.FamilyInstance>(AssertFamilyInstanceEqual, );
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
    [Trait("Column", "ColumnToSpeckle")]
    public async Task ColumnColumnToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Column", "ColumnToNative")]
    public async Task ColumnColumnToNative()
    {
      await SpeckleToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual, );
    }

    [Fact]
    [Trait("Column", "ColumnSelection")]
    public async Task ColumnColumnSelectionToNative()
    {
      await SelectionToNative<DB.FamilyInstance>(AssertFamilyInstanceEqual, );
    }

    [Fact]
    [Trait("Column", "ColumnToNativeUpdates")]
    public async Task ColumnColumnToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.FamilyInstance>(AssertFamilyInstanceEqual, );
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
    [Trait("Curve", "CurveToSpeckle")]
    public async Task CurveCurveToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Curve", "CurveToNative")]
    public async Task CurveCurveToNative()
    {
      await SpeckleToNative<DB.CurveElement>(AssertCurveEqual, );
    }

    [Fact]
    [Trait("Curve", "CurveSelection")]
    public async Task CurveCurveSelectionToNative()
    {
      await SelectionToNative<DB.CurveElement>(AssertCurveEqual, );
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
    [Trait("DirectShape", "DirectShapeToSpeckle")]
    public async Task DirectShapeDirectShapeToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("DirectShape", "DirectShapeToNative")]
    public async Task DirectShapeDirectShapeToNative()
    {
      await SpeckleToNative<DB.DirectShape>(DirectShapeEqual, );
    }

    [Fact]
    [Trait("DirectShape", "DirectShapeSelection")]
    public async Task DirectShapeDirectShapeSelectionToNative()
    {
      await SelectionToNative<DB.DirectShape>(DirectShapeEqual, );
    }

  }

  public class DuctDuct.ExpectedFailures.jsonFixture : SpeckleConversionFixture
  {
    public override string Category => "Duct";
    public override string TestName => "Duct.ExpectedFailures.json";

    public DuctDuct.ExpectedFailures.jsonFixture() : base()
    {
    }
  }

  public class DuctDuct.ExpectedFailures.jsonTests : SpeckleConversionTest, IClassFixture<DuctDuct.ExpectedFailures.jsonFixture>
  {
    public DuctDuct.ExpectedFailures.jsonTests(DuctDuct.ExpectedFailures.jsonFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Duct", "Duct.ExpectedFailures.jsonToSpeckle")]
    public async Task DuctDuct.ExpectedFailures.jsonToSpeckle()
    {
      await NativeToSpeckle();
    }

  }

  public class DuctDuctFixture : SpeckleConversionFixture
  {
    public override string Category => "Duct";
    public override string TestName => "Duct";

    public DuctDuctFixture() : base()
    {
    }
  }

  public class DuctDuctTests : SpeckleConversionTest, IClassFixture<DuctDuctFixture>
  {
    public DuctDuctTests(DuctDuctFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Duct", "DuctToSpeckle")]
    public async Task DuctDuctToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Duct", "DuctToNative")]
    public async Task DuctDuctToNative()
    {
      await SpeckleToNative<DB.Duct>(AssertDuctEqual, );
    }

    [Fact]
    [Trait("Duct", "DuctSelection")]
    public async Task DuctDuctSelectionToNative()
    {
      await SelectionToNative<DB.Duct>(AssertDuctEqual, );
    }

  }

  public class FamilyInstanceFamilyInstance.ExpectedFailures.jsonFixture : SpeckleConversionFixture
  {
    public override string Category => "FamilyInstance";
    public override string TestName => "FamilyInstance.ExpectedFailures.json";

    public FamilyInstanceFamilyInstance.ExpectedFailures.jsonFixture() : base()
    {
    }
  }

  public class FamilyInstanceFamilyInstance.ExpectedFailures.jsonTests : SpeckleConversionTest, IClassFixture<FamilyInstanceFamilyInstance.ExpectedFailures.jsonFixture>
  {
    public FamilyInstanceFamilyInstance.ExpectedFailures.jsonTests(FamilyInstanceFamilyInstance.ExpectedFailures.jsonFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("FamilyInstance", "FamilyInstance.ExpectedFailures.jsonToSpeckle")]
    public async Task FamilyInstanceFamilyInstance.ExpectedFailures.jsonToSpeckle()
    {
      await NativeToSpeckle();
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
    [Trait("FamilyInstance", "FamilyInstanceToSpeckle")]
    public async Task FamilyInstanceFamilyInstanceToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("FamilyInstance", "FamilyInstanceToNative")]
    public async Task FamilyInstanceFamilyInstanceToNative()
    {
      await SpeckleToNative<DB.Element>(AssertNestedEqual, );
    }

    [Fact]
    [Trait("FamilyInstance", "FamilyInstanceSelection")]
    public async Task FamilyInstanceFamilyInstanceSelectionToNative()
    {
      await SelectionToNative<DB.Element>(AssertNestedEqual, );
    }

    [Fact]
    [Trait("FamilyInstance", "FamilyInstanceToNativeUpdates")]
    public async Task FamilyInstanceFamilyInstanceToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.Element>(AssertNestedEqual, );
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
    [Trait("Floor", "FloorToSpeckle")]
    public async Task FloorFloorToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Floor", "FloorToNative")]
    public async Task FloorFloorToNative()
    {
      await SpeckleToNative<DB.Floor>(null, AssertFloorEqual);
    }

    [Fact]
    [Trait("Floor", "FloorSelection")]
    public async Task FloorFloorSelectionToNative()
    {
      await SelectionToNative<DB.Floor>(null, AssertFloorEqual);
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
    [Trait("Opening", "OpeningToSpeckle")]
    public async Task OpeningOpeningToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Opening", "OpeningToNative")]
    public async Task OpeningOpeningToNative()
    {
      await SpeckleToNative<DB.Element>(AssertOpeningEqual, );
    }

    [Fact]
    [Trait("Opening", "OpeningSelection")]
    public async Task OpeningOpeningSelectionToNative()
    {
      await SelectionToNative<DB.Element>(AssertOpeningEqual, );
    }

  }

  public class PipePipeFixture : SpeckleConversionFixture
  {
    public override string Category => "Pipe";
    public override string TestName => "Pipe";

    public PipePipeFixture() : base()
    {
    }
  }

  public class PipePipeTests : SpeckleConversionTest, IClassFixture<PipePipeFixture>
  {
    public PipePipeTests(PipePipeFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Pipe", "PipeToSpeckle")]
    public async Task PipePipeToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Pipe", "PipeToNative")]
    public async Task PipePipeToNative()
    {
      await SpeckleToNative<DB.Pipe>(AssertPipeEqual, );
    }

    [Fact]
    [Trait("Pipe", "PipeSelection")]
    public async Task PipePipeSelectionToNative()
    {
      await SelectionToNative<DB.Pipe>(AssertPipeEqual, );
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
    [Trait("Roof", "RoofToSpeckle")]
    public async Task RoofRoofToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Roof", "RoofToNative")]
    public async Task RoofRoofToNative()
    {
      await SpeckleToNative<DB.RoofBase>(AssertRoofEqual, );
    }

    [Fact]
    [Trait("Roof", "RoofSelection")]
    public async Task RoofRoofSelectionToNative()
    {
      await SelectionToNative<DB.RoofBase>(AssertRoofEqual, );
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
    [Trait("Schedule", "ScheduleToSpeckle")]
    public async Task ScheduleScheduleToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Schedule", "ScheduleToNative")]
    public async Task ScheduleScheduleToNative()
    {
      await SpeckleToNative<DB.ViewSchedule>(, AssertSchedulesEqual);
    }

    [Fact]
    [Trait("Schedule", "ScheduleSelection")]
    public async Task ScheduleScheduleSelectionToNative()
    {
      await SelectionToNative<DB.ViewSchedule>(, AssertSchedulesEqual);
    }

    [Fact]
    [Trait("Schedule", "ScheduleToNativeUpdates")]
    public async Task ScheduleScheduleToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.ViewSchedule>(, AssertSchedulesEqual);
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
    [Trait("Wall", "WallToSpeckle")]
    public async Task WallWallToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Wall", "WallToNative")]
    public async Task WallWallToNative()
    {
      await SpeckleToNative<DB.Wall>(AssertWallEqual, );
    }

    [Fact]
    [Trait("Wall", "WallSelection")]
    public async Task WallWallSelectionToNative()
    {
      await SelectionToNative<DB.Wall>(AssertWallEqual, );
    }

    [Fact]
    [Trait("Wall", "WallToNativeUpdates")]
    public async Task WallWallToNativeUpdates()
    {
      await SpeckleToNativeUpdates<DB.Wall>(AssertWallEqual, );
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
    [Trait("Wire", "WireToSpeckle")]
    public async Task WireWireToSpeckle()
    {
      await NativeToSpeckle();
    }

    [Fact]
    [Trait("Wire", "WireToNative")]
    public async Task WireWireToNative()
    {
      await SpeckleToNative<DB.Wire>(AssertWireEqual, );
    }

    [Fact]
    [Trait("Wire", "WireSelection")]
    public async Task WireWireSelectionToNative()
    {
      await SelectionToNative<DB.Wire>(AssertWireEqual, );
    }

  }

}
