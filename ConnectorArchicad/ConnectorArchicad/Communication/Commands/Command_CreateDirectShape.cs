using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Models;

namespace Archicad.Communication.Commands
{
  sealed internal class CreateDirectShapes : ICommand<IEnumerable<ApplicationObject>>
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

      [JsonProperty("applicationObjects")]
      public IEnumerable<ApplicationObject> ApplicationObjects { get; private set; }

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

    public async Task<IEnumerable<ApplicationObject>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("CreateDirectShapes", new Parameters(Models));
      return result.ApplicationObjects;
    }

    #endregion
  }
}
