using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Logging;

namespace Archicad.Communication.Commands
{
  sealed internal class FinishReceiveTransaction : ICommand<object>
  {
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters { }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result { }

    public async Task<object> Execute(CumulativeTimer cumulativeTimer)
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
        "FinishReceiveTransaction",
        new Parameters(),
        cumulativeTimer
      );
      return result;
    }
  }
}
