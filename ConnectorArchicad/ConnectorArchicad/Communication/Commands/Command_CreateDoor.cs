using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateDoor : ICommand<IEnumerable<string>>
  {

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("subElements")]
      private IEnumerable<ArchicadDoor> Datas { get; }

      public Parameters(IEnumerable<ArchicadDoor> datas)
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
    private IEnumerable<ArchicadDoor> Datas { get; }

    public CreateDoor(IEnumerable<ArchicadDoor> datas)
    {
      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateDoor", new Parameters(Datas));
      return result == null ? null : result.ApplicationIds;
    }

  }
}
