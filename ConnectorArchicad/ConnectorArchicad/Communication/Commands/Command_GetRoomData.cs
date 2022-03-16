using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;

namespace Archicad.Communication.Commands
{
  sealed internal class GetRoomData : ICommand<IEnumerable<Room>>
  {

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {

      [JsonProperty("applicationIds")]
      private IEnumerable<string> ApplicationIds { get; }

      public Parameters(IEnumerable<string> applicationIds)
      {
        ApplicationIds = applicationIds;
      }

    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {

      [JsonProperty("zones")]
      public IEnumerable<Room> Rooms { get; private set; }

    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetRoomData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<Room>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("GetRoomData", new Parameters(ApplicationIds));
      foreach (var room in result.Rooms)
        room.units = Units.Meters;

      return result.Rooms;
    }

  }
}
