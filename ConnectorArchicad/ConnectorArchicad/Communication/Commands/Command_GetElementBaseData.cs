using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectorArchicad.Communication.Commands;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class GetElementBaseData : GetDataBase, ICommand<IEnumerable<DirectShape>>
{
  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("elements")]
    public IEnumerable<DirectShape> Datas { get; private set; }
  }

  public GetElementBaseData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<IEnumerable<DirectShape>> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "GetElementBaseData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );
    foreach (var directShape in result.Datas)
    {
      directShape.units = Units.Meters;
    }

    return result.Datas;
  }
}
