using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class GetModelForElements : ICommand<IEnumerable<Model.ElementModelData>>
  {
    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      #region --- Fields ---

      [JsonProperty("elementIds")]
      private IEnumerable<string> ElementIds { get; }

      #endregion

      #region --- Ctor \ Dtor ---

      public Parameters(IEnumerable<string> elementIds)
      {
        ElementIds = elementIds;
      }

      #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      #region --- Fields ---

      [JsonProperty("models")]
      public IEnumerable<Model.ElementModelData> Models { get; private set; }

      #endregion
    }

    #endregion

    #region --- Fields ---

    private IEnumerable<string> ElementIds { get; }

    #endregion

    #region --- Ctor \ Dtor ---

    public GetModelForElements(IEnumerable<string> elementIds)
    {
      ElementIds = elementIds;
    }

    #endregion

    #region --- Functions ---

    public async Task<IEnumerable<Model.ElementModelData>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetModelForElements", new Parameters(ElementIds));
      return result.Models;
    }

    #endregion
  }
}
