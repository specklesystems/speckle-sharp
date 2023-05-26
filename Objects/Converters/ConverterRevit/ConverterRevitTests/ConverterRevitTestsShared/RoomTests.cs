//using Xunit;
//using System.Threading.Tasks;

//namespace ConverterRevitTests
//{
//  public class RoomFixture : SpeckleConversionFixture
//  {
//    public override string TestFile => Globals.GetTestModelOfCategory(Category, "Room.rvt");
//    public override string Category => TestCategories.Room;
//    public RoomFixture() : base ()
//    {
//    }
//  }

//  public class RoomTests : SpeckleConversionTest, IClassFixture<RoomFixture>
//  {
//    public RoomTests(RoomFixture fixture) : base(fixture)
//    {
//    }

//    [Fact]
//    [Trait("Room", "ToSpeckle")]
//    public async Task RoomToSpeckle()
//    {
//      await NativeToSpeckle();
//    }
//  }
//}
