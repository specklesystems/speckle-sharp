using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class GetObjectData : ICommand<IEnumerable<ArchicadObject>>
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
      [JsonProperty("objects")]
      public IEnumerable<ArchicadObject> Datas { get; private set; }
    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetObjectData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadObject>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
        "GetObjectData",
        new Parameters(ApplicationIds)
      );
      foreach (var @object in result.Datas)
        @object.units = Units.Meters;

      return result.Datas;
    }
  }
}
