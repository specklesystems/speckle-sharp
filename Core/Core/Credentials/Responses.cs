namespace Speckle.Core.Credentials
{

  public class UserServerInfoResponse
  {
    public UserInfo user { get; set; }
    public ServerInfo serverInfo { get; set; }
  }
  public class UserInfoResponse
  {
    public UserInfo user { get; set; }
  }

  public class UserInfo
  {
    public string id { get; set; }
    public string name { get; set; }
    public string email { get; set; }
    public string company { get; set; }
    public string avatar { get; set; }
  }

  public class ServerInfoResponse
  {
    public ServerInfo serverInfo { get; set; }
  }

  public class ServerInfo
  {
    public string name { get; set; }
    public string company { get; set; }
    public string url { get; set; }
  }

  public class TokenExchangeResponse
  {
    public string token { get; set; }
    public string refreshToken { get; set; }
  }
}
