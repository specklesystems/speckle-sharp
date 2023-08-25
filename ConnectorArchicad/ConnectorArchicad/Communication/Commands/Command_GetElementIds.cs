using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Converters;
using Objects.BuiltElements.Archicad;

namespace Archicad.Communication.Commands
{
  internal sealed class GetElementIds : ICommand<IEnumerable<string>>
  {
    public enum ElementFilter
    {
      All,
      Selection,
      ElementType
    }

    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      #region --- Fields ---

      [JsonProperty("elementFilter")]
      [JsonConverter(typeof(StringEnumConverter))]
      private ElementFilter Filter { get; }

      [JsonProperty("filterBy")]
      private List<string>? FilterBy { get; }
      #endregion

      #region --- Ctor \ Dtor ---

      public Parameters(ElementFilter filter, List<string>? filterBy = null)
      {
        Filter = filter;
        FilterBy = filterBy;
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

    private ElementFilter Filter { get; }
    private List<string>? FilterBy { get; }

    #endregion

    #region --- Ctor \ Dtor ---

    public GetElementIds(ElementFilter filter, List<string>? filterBy = null)
    {
      Filter = filter;
      FilterBy = filterBy;
    }

    #endregion

    #region --- Functions ---

    public async Task<IEnumerable<string>> Execute()
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>(
        "GetElementIds",
        new Parameters(Filter, FilterBy)
      );
      return result.ApplicationIds;
    }

    #endregion
  }
}
