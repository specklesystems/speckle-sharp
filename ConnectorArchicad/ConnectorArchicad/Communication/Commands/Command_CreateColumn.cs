using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateColumn : ICommand<IEnumerable<ApplicationObject>>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      [JsonProperty("columns")]
      private IEnumerable<ArchicadColumn> Datas { get; }

      public Parameters(IEnumerable<ArchicadColumn> datas)
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

    private IEnumerable<ArchicadColumn> Datas { get; }

    public CreateColumn(IEnumerable<ArchicadColumn> datas)
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
      var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateColumn", new Parameters(Datas));
      return result == null ? null : result.ApplicationObjects;
    }
  }
}
