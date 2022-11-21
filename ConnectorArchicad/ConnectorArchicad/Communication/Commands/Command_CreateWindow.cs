using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Models;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateWindow : ICommand<IEnumerable<string>>
  {

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("subElements")]
      private IEnumerable<ArchicadWindow> Datas { get; }

      public Parameters(IEnumerable<ArchicadWindow> datas)
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
    private IEnumerable<ArchicadWindow> Datas { get; }

    public CreateWindow(IEnumerable<ArchicadWindow> datas)
    {
      Datas = datas;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateWindow", new Parameters(Datas));
      return result == null ? null : result.ApplicationIds;
    }

  }
}
