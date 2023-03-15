using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateFloor : ICommand<IEnumerable<string>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("slabs")]
      private IEnumerable<ArchicadFloor> Datas { get; }

      public Parameters(IEnumerable<ArchicadFloor> datas)
      {
        Datas = datas;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationIds")]
      public IEnumerable<string> ApplicationIds { get; private set; }
    }

    private IEnumerable<ArchicadFloor> Datas { get; }

    public CreateFloor(IEnumerable<ArchicadFloor> datas)
    {
      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateSlab", new Parameters(Datas));
      return result.ApplicationIds;
    }
  }
}
