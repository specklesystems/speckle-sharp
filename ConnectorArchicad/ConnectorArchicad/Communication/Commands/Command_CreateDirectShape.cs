using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateDirectShapes : ICommand<IEnumerable<string>>
  {
    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      #region --- Fields ---

      [JsonProperty("models")]
      private IEnumerable<Model.ElementModelData> Models { get; }

      #endregion

      #region --- Ctor \ Dtor ---

      public Parameters(IEnumerable<Model.ElementModelData> models)
      {
        Models = models;
      }

      #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      #region --- Fields ---

      [JsonProperty("applicationIds")]
      public IEnumerable<string> ApplicationIds { get; private set; }

      #endregion
    }

    #endregion

    #region --- Fields ---

    private IEnumerable<Model.ElementModelData> Models { get; }

    #endregion

    #region --- Ctor \ Dtor ---

    public CreateDirectShapes(IEnumerable<Model.ElementModelData> models)
    {
      Models = models;
    }

    #endregion

    #region --- Functions ---

    public async Task<IEnumerable<string>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateDirectShapes", new Parameters(Models));
      return result.ApplicationIds;
    }

    #endregion
  }
}
