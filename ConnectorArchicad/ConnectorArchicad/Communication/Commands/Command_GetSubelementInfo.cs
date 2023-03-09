using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Objects.BuiltElements.Archicad;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class GetSubElementInfo : ICommand<IEnumerable<SubElementData>>
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
      public IEnumerable<SubElementData> Datas { get; private set; }

    }

    private string ApplicationId { get; }

    public GetSubElementInfo(string applicationId)
    {
      ApplicationId = applicationId;
    }

    public async Task<IEnumerable<SubElementData>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetSubelementInfo", new Parameters(ApplicationId));
      return result.Datas;
    }

  }
}
