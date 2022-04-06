using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateRoom : ICommand<IEnumerable<string>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("zones")] private IEnumerable<Room> Datas { get; }

      public Parameters(IEnumerable<Room> datas)
      {
        Datas = datas;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationIds")] public IEnumerable<string> ApplicationIds { get; private set; }
    }

    private IEnumerable<Room> Datas { get; }

    public CreateRoom(IEnumerable<Room> datas)
    {
      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateZone", new Parameters(Datas));
      return result?.ApplicationIds;
    }
  }
}
