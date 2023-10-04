using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class GetRoomData : ICommand<IEnumerable<Archicad.Room>>
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
      public IEnumerable<Archicad.Room> Rooms { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetRoomData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<Archicad.Room>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("GetRoomData", new Parameters(ApplicationIds));

      return result.Rooms;
    }
  }
}
