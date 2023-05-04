using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Models;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateRoof : ICommand<IEnumerable<ApplicationObject>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("roofs")]
      private IEnumerable<ArchicadRoof> Roofs { get; }

      public Parameters(IEnumerable<ArchicadRoof> roofs)
      {
        Roofs = roofs;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationObjects")]
      public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }
    }

    private IEnumerable<ArchicadRoof> Roofs { get; }

    public CreateRoof(IEnumerable<ArchicadRoof> roofs)
    {
      foreach (var roof in roofs)
      {
        roof.displayValue = null;
      }

      Roofs = roofs;
    }

    public async Task<IEnumerable<ApplicationObject>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateRoof", new Parameters(Roofs));
      return result == null ? null : result.ApplicationObjects;
    }
  }
}
