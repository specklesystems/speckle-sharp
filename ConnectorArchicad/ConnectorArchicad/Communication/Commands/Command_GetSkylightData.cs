using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class GetSkylightData : ICommand<IEnumerable<ArchicadSkylight>>
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

      [JsonProperty("skylights")]
      public IEnumerable<ArchicadSkylight> Datas { get; private set; }

    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetSkylightData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadSkylight>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetSkylightData", new Parameters(ApplicationIds));
      //foreach (var subelement in result.Datas)
      //subelement.units = Units.Meters;

      return result.Datas;
    }

  }
}
