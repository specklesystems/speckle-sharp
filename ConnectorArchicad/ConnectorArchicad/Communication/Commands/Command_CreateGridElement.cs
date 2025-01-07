using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class CreateGridElement : ICommand<IEnumerable<ApplicationObject>>
{
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters
  {
    [JsonProperty("gridElements")]
    private IEnumerable<Archicad.GridElement> Datas { get; }

    public Parameters(IEnumerable<Archicad.GridElement> datas)
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

  private IEnumerable<Archicad.GridElement> Datas { get; }

  public CreateGridElement(IEnumerable<Archicad.GridElement> datas)
  {
    Datas = datas;
  }

  public async Task<IEnumerable<ApplicationObject>> Execute()
  {
    var result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateGridElement", new Parameters(Datas));
    return result == null ? null : result.ApplicationObjects;
  }
}
