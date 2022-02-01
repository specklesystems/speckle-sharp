using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Objects.BuiltElements.Archicad.Model;

namespace Archicad.Communication.Commands
{
  sealed internal class GetWallData : ICommand<IEnumerable<Wall>>
  {

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {

      [JsonProperty("elementIds")]
      private IEnumerable<string> ElementIds { get; }

      public Parameters(IEnumerable<string> elementIds)
      {
        ElementIds = elementIds;
      }

    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {

      [JsonProperty("walls")]
      public IEnumerable<Wall> Datas { get; private set; }

    }

    private IEnumerable<string> ElementIds { get; }

    public GetWallData(IEnumerable<string> elementIds)
    {
      ElementIds = elementIds;
    }

    public async Task<IEnumerable<Wall>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetWallData", new Parameters(ElementIds));
      return result.Datas;
    }

  }
}
