#nullable disable
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Credentials;

public class ActiveUserServerInfoResponse
{
  public UserInfo activeUser { get; set; }
  public ServerInfo serverInfo { get; set; }
}

public class TokenExchangeResponse
{
  public string token { get; set; }
  public string refreshToken { get; set; }
}
