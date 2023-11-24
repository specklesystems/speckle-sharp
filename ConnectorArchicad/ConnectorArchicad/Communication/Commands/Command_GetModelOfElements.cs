using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

sealed internal class GetModelForElements : ICommand<IEnumerable<Model.ElementModelData>>
{
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters
  {
    [JsonProperty("applicationIds")]
    private IEnumerable<string> ApplicationIds { get; }

    public Parameters(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }
  }

  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    [JsonProperty("models")]
    public IEnumerable<Model.ElementModelData> Models { get; private set; }
  }

  private IEnumerable<string> ApplicationIds { get; }

  public GetModelForElements(IEnumerable<string> applicationIds)
  {
    ApplicationIds = applicationIds;
  }

  public async Task<IEnumerable<Model.ElementModelData>> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "GetModelForElements",
      new Parameters(ApplicationIds)
    );
    return result.Models;
  }
}
