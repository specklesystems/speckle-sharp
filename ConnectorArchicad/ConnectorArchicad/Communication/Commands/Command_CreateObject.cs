using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateObject : ICommand<IEnumerable<string>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("objects")]
      private IEnumerable<ArchicadObject> Objects { get; }

      public Parameters(IEnumerable<ArchicadObject> objects)
      {
        Objects = objects;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationIds")]
      public IEnumerable<string> ApplicationIds { get; private set; }
    }

    private IEnumerable<ArchicadObject> Objects { get; }

    public CreateObject(IEnumerable<ArchicadObject> objects)
    {
      Objects = objects;
    }

    public async Task<IEnumerable<string>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateObject", new Parameters(Objects));
      return result == null ? null : result.ApplicationIds;
    }

  }
}
