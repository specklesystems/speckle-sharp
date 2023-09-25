using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  internal sealed class GetFloorData : ICommand<IEnumerable<ArchicadFloor>>
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
      [JsonProperty("slabs")]
      public IEnumerable<ArchicadFloor> Datas { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetFloorData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadFloor>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
        "GetSlabData",
        new Parameters(ApplicationIds)
      );
      foreach (var floor in result.Datas)
        floor.units = Units.Meters;

      return result.Datas;
    }
  }
}
