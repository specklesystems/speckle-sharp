using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class GetBeamData : ICommand<IEnumerable<ArchicadBeam>>
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
      [JsonProperty("beams")]
      public IEnumerable<ArchicadBeam> Datas { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetBeamData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadBeam>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
        "GetBeamData",
        new Parameters(ApplicationIds)
      );
      foreach (var beam in result.Datas)
        beam.units = Units.Meters;

      return result.Datas;
    }
  }
}
