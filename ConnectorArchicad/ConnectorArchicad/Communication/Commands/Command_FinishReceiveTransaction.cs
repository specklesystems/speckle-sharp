using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class FinishReceiveTransaction : ICommand<object>
{
  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters { }

  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result { }

  public async Task<object> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>("FinishReceiveTransaction", new Parameters());
    return result;
  }
}
