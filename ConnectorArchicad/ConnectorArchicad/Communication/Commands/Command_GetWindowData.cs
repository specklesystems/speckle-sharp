using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectorArchicad.Communication.Commands;

namespace Archicad.Communication.Commands;

internal sealed class GetWindowData : GetDataBase, ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
{
  public GetWindowData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
  {
    dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
      "GetWindowData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return (Speckle.Newtonsoft.Json.Linq.JArray)result["windows"];
  }
}
