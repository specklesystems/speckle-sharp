using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateWall : ICommand<IEnumerable<ApplicationObject>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("walls")]
      private IEnumerable<ArchicadWall> Datas { get; }

      public Parameters(IEnumerable<ArchicadWall> datas)
      {
        Datas = datas;
      }
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      [JsonProperty("applicationObjects")]
      public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }
    }

    private IEnumerable<ArchicadWall> Datas { get; }

    public CreateWall(IEnumerable<ArchicadWall> datas)
    {
      foreach (var data in datas)
      {
        data.displayValue = null;
        data.baseLine = null;
      }

      Datas = datas;
    }

    public async Task<IEnumerable<ApplicationObject>> Execute()
    {
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateWall", new Parameters(Datas));
      return result == null ? null : result.ApplicationObjects;
    }
  }
}
