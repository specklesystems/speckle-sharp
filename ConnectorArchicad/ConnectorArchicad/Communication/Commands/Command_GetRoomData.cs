using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class GetRoomData : ICommand<IEnumerable<ArchicadRoom>>
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
      public IEnumerable<ArchicadRoom> Rooms { get; private set; }

    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetRoomData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadRoom>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("GetRoomData", new Parameters(ApplicationIds));
      foreach (var room in result.Rooms)
        room.units = Units.Meters;

      return result.Rooms;
    }

  }
}
