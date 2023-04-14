using System;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using System.Collections.Generic;

using Xunit;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public class RoomFixture : SpeckleConversionFixture
  {
    public override string TestFile => Globals.GetTestModel("Room.rvt");
   // public override string NewFile => Globals.GetTestModel("Room_ToNative.rvt");
    public override List<BuiltInCategory> Categories => new List<BuiltInCategory> { BuiltInCategory.OST_Rooms };
    public RoomFixture() : base ()
    {
    }
  }

  public class RoomTests : SpeckleConversionTest, IClassFixture<RoomFixture>
  {
    public RoomTests(RoomFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Room", "ToSpeckle")]
    public async Task RoomToSpeckle()
    {
      await NativeToSpeckle();
    }


  }
}
