using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands;

internal sealed class GetElementsType : ICommand<Dictionary<string, IEnumerable<string>>>
{
  #region --- Classes ---

  [JsonObject(MemberSerialization.OptIn)]
  public sealed class Parameters
  {
    #region --- Fields ---

    [JsonProperty("applicationIds")]
    private IEnumerable<string> ApplicationIds { get; }

    #endregion

    #region --- Ctor \ Dtor ---

    public Parameters(IEnumerable<string> applicationIds)
    {
      ApplicationIds = applicationIds;
    }

    #endregion
  }

  [JsonObject(MemberSerialization.OptIn)]
  private sealed class Result
  {
    #region --- Fields ---

    [JsonProperty("elementTypes")]
    public IEnumerable<TypeDescription> ElementTypes { get; private set; }

    #endregion
  }

  [JsonObject(MemberSerialization.OptIn)]
  private sealed class TypeDescription
  {
    [JsonProperty("applicationId")]
    public string ApplicationId { get; private set; }

    [JsonProperty("elementType")]
    public string ElementType { get; private set; }
  }

  #endregion

  #region --- Fields ---

  private IEnumerable<string> ApplicationIds { get; }

  #endregion

  #region --- Ctor \ Dtor ---

  public GetElementsType(IEnumerable<string> applicationIds)
  {
    ApplicationIds = applicationIds;
  }

  #endregion

  #region --- Functions ---

  public async Task<Dictionary<string, IEnumerable<string>>> Execute()
  {
    Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
      "GetElementTypes",
      new Parameters(ApplicationIds)
    );
    return result.ElementTypes
      .GroupBy(row => row.ElementType)
      .ToDictionary(group => group.Key, group => group.Select(x => x.ApplicationId));
  }

  #endregion
}
