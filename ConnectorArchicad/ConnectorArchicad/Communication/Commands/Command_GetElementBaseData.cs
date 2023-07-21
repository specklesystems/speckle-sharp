using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class GetElementBaseData : ICommand<IEnumerable<DirectShape>>
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
      [JsonProperty("elements")]
      public IEnumerable<DirectShape> Datas { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetElementBaseData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<DirectShape>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
        "GetElementBaseData",
        new Parameters(ApplicationIds)
      );
      foreach (var directShape in result.Datas)
        directShape.units = Units.Meters;

      return result.Datas;
    }
  }
}
