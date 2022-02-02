using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;

namespace Archicad.Communication.Commands
{
  internal sealed class GetCeilingData : ICommand<IEnumerable<Ceiling>>
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
      [JsonProperty("slabs")]
      public IEnumerable<Ceiling> Datas { get; private set; }
    }

    private IEnumerable<string> ElementIds { get; }

    public GetCeilingData(IEnumerable<string> elementIds)
    {
      ElementIds = elementIds;
    }

    public async Task<IEnumerable<Ceiling>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetSlabData", new Parameters(ElementIds));
      foreach (var ceiling in result.Datas)
        ceiling.units = Units.Meters;

      return result.Datas;
    }

  }
}
