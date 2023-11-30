using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements;

namespace Archicad.Communication.Commands;

sealed internal class GetGridElementData : ICommand<IEnumerable<Archicad.GridElement>>
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
    [JsonProperty("gridElements")]
    public IEnumerable<Archicad.GridElement> Datas { get; private set; }
  }

  private IEnumerable<string> ApplicationIds { get; }

  public GetGridElementData(IEnumerable<string> applicationIds)
  {
    ApplicationIds = applicationIds;
  }

  public async Task<IEnumerable<Archicad.GridElement>> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "GetGridElementData",
      new Parameters(ApplicationIds)
    );

    return result.Datas;
  }
}
