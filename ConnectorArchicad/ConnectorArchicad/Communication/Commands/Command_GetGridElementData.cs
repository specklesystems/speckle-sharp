using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;
using ConnectorArchicad.Communication.Commands;

namespace Archicad.Communication.Commands;

sealed internal class GetGridElementData : GetDataBase, ICommand<IEnumerable<Archicad.GridElement>>
{
  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("gridElements")]
    public IEnumerable<Archicad.GridElement> Datas { get; private set; }
  }

  public GetGridElementData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, false, false) // common GridLine class in Objects kit has no Archicad-related properties
  { }

  public async Task<IEnumerable<Archicad.GridElement>> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "GetGridElementData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return result.Datas;
  }
}
