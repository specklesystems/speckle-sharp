using Newtonsoft.Json;

namespace Archicad.Communication
{
  [JsonObject(MemberSerialization.OptIn)]
  internal sealed class AddOnCommandRequest<T> where T : class
  {
    #region --- Classes ---

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class AddonCommandID
    {
      #region --- Fields ---

      [JsonProperty("commandName")]
      public string CommandName { get; private set; }

      [JsonProperty("commandNamespace")]
      public string CommandNamespace { get; } = "Speckle";

      #endregion

      #region  --- Ctor \ Dtor ---

      public AddonCommandID(string commandName)
      {
        CommandName = commandName;
      }

      #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class AddonCommandParameters
    {
      #region --- Fields ---

      [JsonProperty("addOnCommandId")]
      private AddonCommandID Id { get; set; }

      [JsonProperty("addOnCommandParameters")]
      private T Parameters { get; set; }

      #endregion

      #region  --- Ctor \ Dtor ---

      public AddonCommandParameters(string commandName, T parameters)
      {
        Id = new AddonCommandID(commandName);
        Parameters = parameters;
      }

      #endregion
    }

    #endregion

    #region --- Fields ---

    [JsonProperty("command")]
    private string Command { get; } = "API.ExecuteAddOnCommand";

    [JsonProperty("parameters")]
    private AddonCommandParameters Parameters { get; set; }

    #endregion

    #region  --- Ctor \ Dtor ---

    public AddOnCommandRequest(string commandName, T requestParams)
    {
      Parameters = new AddonCommandParameters(commandName, requestParams);
    }

    #endregion
  }
}
