using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  internal sealed class GetRoofData : ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
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

    private IEnumerable<string> ApplicationIds { get; }

    public GetRoofData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
    {
      dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
        "GetRoofData",
        new Parameters(ApplicationIds)
      );

      return (Speckle.Newtonsoft.Json.Linq.JArray)result["roofs"];
    }
  }
}
