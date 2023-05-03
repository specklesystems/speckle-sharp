using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  internal sealed class GetShellData : ICommand<IEnumerable<ArchicadShell>>
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
      [JsonProperty("Shells")]
      public IEnumerable<ArchicadShell> Datas { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetShellData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadShell>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetShellData", new Parameters(ApplicationIds));
      foreach (var shell in result.Datas)
        shell.units = Units.Meters;

      return result.Datas;
    }

  }
}
