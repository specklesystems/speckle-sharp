using System.Collections.Generic;
using Speckle.Newtonsoft.Json;

namespace ConnectorArchicad.Communication.Commands;

internal class GetDataBase
{
  [JsonObject(MemberSerialization.OptIn)]
  internal class Parameters
  {
    [JsonProperty("applicationIds")]
    protected IEnumerable<string> ApplicationIds { get; }

    [JsonProperty("sendProperties")]
    protected bool SendProperties { get; }

    [JsonProperty("sendListingParameters")]
    protected bool SendListingParameters { get; }

    public Parameters(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
    {
      ApplicationIds = applicationIds;
      SendProperties = sendProperties;
      SendListingParameters = sendListingParameters;
    }
  }

  protected IEnumerable<string> ApplicationIds { get; }
  protected bool SendProperties { get; }
  protected bool SendListingParameters { get; }

  public GetDataBase(IEnumerable<string> applicationIds, bool sendProperties, bool sendListingParameters)
  {
    ApplicationIds = applicationIds;
    SendProperties = sendProperties;
    SendListingParameters = sendListingParameters;
  }
}
