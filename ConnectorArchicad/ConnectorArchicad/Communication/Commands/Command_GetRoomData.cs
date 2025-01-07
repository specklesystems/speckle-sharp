using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectorArchicad.Communication.Commands;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class GetRoomData : GetDataBase, ICommand<IEnumerable<Archicad.Room>>
{
  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("zones")]
    public IEnumerable<Archicad.Room> Rooms { get; private set; }
  }

  public GetRoomData(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    : base(applicationIds, sendProperties, sendListingParameters) { }

  public async Task<IEnumerable<Archicad.Room>> Execute()
  {
    var result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "GetRoomData",
      new Parameters(ApplicationIds, SendProperties, SendListingParameters)
    );

    return result.Rooms;
  }
}
