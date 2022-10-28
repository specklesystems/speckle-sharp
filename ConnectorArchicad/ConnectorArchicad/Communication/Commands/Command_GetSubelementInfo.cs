using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;

namespace Archicad.Communication.Commands
{
  sealed internal class GetSubelementInfo : ICommand<IEnumerable<SubelementModelData>>
  {

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {

      [JsonProperty("applicationId")]
      private string ApplicationId { get; }

      public Parameters(string applicationId)
      {
        ApplicationId = applicationId;
      }

    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {

      [JsonProperty("subelementModels")]
      public IEnumerable<SubelementModelData> Datas { get; private set; }

    }

    private string ApplicationId { get; }

    public GetSubelementInfo(string applicationId)
    {
      ApplicationId = applicationId;
    }

    public async Task<IEnumerable<SubelementModelData>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetSubelementInfo", new Parameters(ApplicationId));
      return result.Datas;
    }

  }
}
