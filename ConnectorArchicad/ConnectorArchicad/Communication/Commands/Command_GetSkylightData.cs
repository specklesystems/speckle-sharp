using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectorArchicad.Communication.Commands;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

sealed internal class GetSkylightData : GetDataBase, ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
{
  public GetSkylightData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
  {
    dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
      "GetSkylightData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return (Speckle.Newtonsoft.Json.Linq.JArray)result["skylights"];
  }
}
