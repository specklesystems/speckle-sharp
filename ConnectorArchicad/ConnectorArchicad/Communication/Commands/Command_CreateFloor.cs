using System.Collections.Generic;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class CreateFloor : ICommand<IEnumerable<ApplicationObject>>
{
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters
  {
    [JsonProperty("slabs")]
    private IEnumerable<ArchicadFloor> Datas { get; }

    public Parameters(IEnumerable<ArchicadFloor> datas)
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

  private IEnumerable<ArchicadFloor> Datas { get; }

  public CreateFloor(IEnumerable<ArchicadFloor> datas)
  {
    Datas = datas;
  }

  public async Task<IEnumerable<ApplicationObject>> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateSlab", new Parameters(Datas));
    return result == null ? null : result.ApplicationObjects;
  }
}
