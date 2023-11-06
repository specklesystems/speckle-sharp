using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  internal sealed class GetShellData : ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
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

    public GetShellData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
    {
      dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
        "GetShellData",
        new Parameters(ApplicationIds)
      );

      return (Speckle.Newtonsoft.Json.Linq.JArray)result["shells"];
    }
  }
}
