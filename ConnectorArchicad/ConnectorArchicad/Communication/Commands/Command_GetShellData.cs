using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectorArchicad.Communication.Commands;

namespace Archicad.Communication.Commands;

internal sealed class GetShellData : GetDataBase, ICommand<Speckle.Newtonsoft.Json.Linq.JArray>
{
  public GetShellData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<Speckle.Newtonsoft.Json.Linq.JArray> Execute()
  {
    dynamic result = await HttpCommandExecutor.Execute<Parameters, dynamic>(
      "GetShellData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return (Speckle.Newtonsoft.Json.Linq.JArray)result["shells"];
  }
}
