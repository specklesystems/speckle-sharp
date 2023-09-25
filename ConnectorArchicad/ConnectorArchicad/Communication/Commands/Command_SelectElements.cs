using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication.Commands
{
  internal sealed class SelectElements : ICommand<object>
  {
    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      #region --- Fields ---

      [JsonProperty("applicationIds")]
      private IEnumerable<string> ElementIds { get; }

      [JsonProperty("deselect")]
      private bool Deselect { get; }

      [JsonProperty("clearSelection")]
      private bool ClearSelection { get; }

      #endregion

      #region --- Ctor \ Dtor ---

      public Parameters(IEnumerable<string> elementIds, bool deselect)
      {
        ElementIds = elementIds;
        Deselect = deselect;
        ClearSelection = !deselect;
      }

      #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class Result
    {
      #region --- Fields ---

      #endregion
    }

    #endregion

    #region --- Fields ---

    private IEnumerable<string> ElementIds { get; }

    private bool Deselect { get; }

    #endregion

    #region --- Ctor \ Dtor ---

    public SelectElements(IEnumerable<string> elementIds, bool deselect)
    {
      ElementIds = elementIds;
      Deselect = deselect;
    }

    #endregion

    #region --- Functions ---

    public async Task<object> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("SelectElements", new Parameters(ElementIds, Deselect));
      return result;
    }

    #endregion
  }
}
