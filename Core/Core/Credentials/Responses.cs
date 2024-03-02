#nullable disable
using Speckle.Core.Api;

namespace Speckle.Core.Credentials;

public class ActiveUserServerInfoResponse
{
  public UserInfo activeUser { get; set; }
  public ServerInfo serverInfo { get; set; }
}

public class ActiveUserResponse
{
  public UserInfo activeUser { get; set; }
}

public class UserInfo
{
  public string id { get; set; }
  public string name { get; set; }
  public string email { get; set; }
  public string company { get; set; }
  public string avatar { get; set; }

  public Streams streams { get; set; }
  public Commits commits { get; set; }
}

public class TokenExchangeResponse
{
  public string token { get; set; }
  public string refreshToken { get; set; }
}

public class Streams
{
  public int totalCount { get; set; }
}

public class Commits
{
  public int totalCount { get; set; }
}
