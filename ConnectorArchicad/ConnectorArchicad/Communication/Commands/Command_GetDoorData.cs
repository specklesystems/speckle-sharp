using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;

namespace Archicad.Communication.Commands
{
  sealed internal class GetDoorData : ICommand<IEnumerable<ArchicadDoor>>
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

      [JsonProperty("doors")]
      public IEnumerable<ArchicadDoor> Datas { get; private set; }

    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetDoorData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadDoor>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetDoorData", new Parameters(ApplicationIds));
      //foreach (var subelement in result.Datas)
        //subelement.units = Units.Meters;

      return result.Datas;
    }

  }
}
