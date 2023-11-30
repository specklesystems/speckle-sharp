using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class GetProjectInfo : ICommand<Model.ProjectInfoData>
{
  #region --- Classes ---

  [JsonObject]
  public sealed class Parameters { }

  #endregion

  #region --- Functions ---

  public async Task<Model.ProjectInfoData> Execute()
  {
    return await HttpCommandExecutor.Execute<Parameters, Model.ProjectInfoData>("GetProjectInfo", null);
  }

  #endregion
}
