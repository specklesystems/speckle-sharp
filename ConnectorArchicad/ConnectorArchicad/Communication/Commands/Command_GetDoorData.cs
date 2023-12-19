using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using ConnectorArchicad.Communication.Commands;

namespace Archicad.Communication.Commands;

sealed internal class GetDoorData : GetDataBase, ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
{
  public GetDoorData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
  {
    dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
      "GetDoorData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return (Speckle.Newtonsoft.Json.Linq.JArray)result["doors"];
  }
}
