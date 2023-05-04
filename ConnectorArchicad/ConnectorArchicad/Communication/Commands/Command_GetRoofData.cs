using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  internal sealed class GetRoofData : ICommand<IEnumerable<ArchicadRoof>>
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
      [JsonProperty("Roofs")]
      public IEnumerable<ArchicadRoof> Datas { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetRoofData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadRoof>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetRoofData", new Parameters(ApplicationIds));
      foreach (var roof in result.Datas)
        roof.units = Units.Meters;

      return result.Datas;
    }

  }
}
