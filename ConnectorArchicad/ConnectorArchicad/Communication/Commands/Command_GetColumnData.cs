using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;


namespace Archicad.Communication.Commands
{
  sealed internal class GetColumnData : ICommand<IEnumerable<ArchicadColumn>>
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

      [JsonProperty("columns")]
      public IEnumerable<ArchicadColumn> Datas { get; private set; }

    }

    private IEnumerable<string> ApplicationIds { get; }

    public GetColumnData(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    public async Task<IEnumerable<ArchicadColumn>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetColumnData", new Parameters(ApplicationIds));
      foreach (var beam in result.Datas)
        beam.units = Units.Meters;

      return result.Datas;
    }

  }
}
