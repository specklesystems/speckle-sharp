using Newtonsoft.Json;

namespace Archicad.Communication
{
  [JsonObject(MemberSerialization.OptIn)]
  internal sealed class AddOnCommandResponse<T> where T : class
  {
    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class AddOnCommandResult
    {
      #region --- Fields ---

      [JsonProperty("addOnCommandResponse")]
      public T Response { get; private set; }

      #endregion
    }

    #endregion

    #region --- Fields ---

    [JsonProperty("succeeded")]
    public bool Succeeded { get; private set; }

    [JsonProperty("result")]
    private AddOnCommandResult Response { get; set; }

    #endregion

    #region --- Properties ---

    public T Result
    {
      get { return Response?.Response; }
    }

    #endregion
  }
}
