using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Converters;
using Speckle.Core.Logging;

namespace Archicad.Communication.Commands
{
  internal sealed class GetElementIds : ICommand<IEnumerable<string>>
  {
    public enum ElementFilter
    {
      All,
      Selection
    }

    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Parameters
    {
      #region --- Fields ---

      [JsonProperty("elementFilter")]
      [JsonConverter(typeof(StringEnumConverter))]
      private ElementFilter Filter { get; }

      #endregion

      #region --- Ctor \ Dtor ---

      public Parameters(ElementFilter filter)
      {
        Filter = filter;
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

    #endregion

    #region --- Ctor \ Dtor ---

    public GetElementIds(ElementFilter filter)
    {
      Filter = filter;
    }

    #endregion

    #region --- Functions ---

    public async Task<IEnumerable<string>> Execute(CumulativeTimer cumulativeTimer)
    {
      Result result = await HttpCommandExecutor.Execute<Parameters, Result>("GetElementIds", new Parameters(Filter), cumulativeTimer);
      return result.ApplicationIds;
    }

    #endregion
  }
}
