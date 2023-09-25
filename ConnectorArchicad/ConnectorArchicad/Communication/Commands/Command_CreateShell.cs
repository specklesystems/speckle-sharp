using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Models;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateShell : ICommand<IEnumerable<ApplicationObject>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("shells")]
      private IEnumerable<ArchicadShell> Shells { get; }

      public Parameters(IEnumerable<ArchicadShell> shells)
      {
        Shells = shells;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationObjects")]
      public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }
    }

    private IEnumerable<ArchicadShell> Shells { get; }

    public CreateShell(IEnumerable<ArchicadShell> shells)
    {
      foreach (var shell in shells)
      {
        shell.displayValue = null;
      }

      Shells = shells;
    }

    public async Task<IEnumerable<ApplicationObject>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateShell", new Parameters(Shells));
      return result == null ? null : result.ApplicationObjects;
    }
  }
}
