using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectorArchicad.Communication.Commands;
using Objects.BuiltElements.Archicad;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

sealed internal class GetElementBaseData : GetDataBase, ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
{
  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("elements")]
    public IEnumerable<DirectShape> Datas { get; private set; }
  }

  public GetElementBaseData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
  {
    dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
      "GetElementBaseData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return (Speckle.Newtonsoft.Json.Linq.JArray)result["elements"];
  }
}
