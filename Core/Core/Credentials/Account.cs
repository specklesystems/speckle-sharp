#nullable disable
using System;
using System.Threading.Tasks;
using Speckle.Core.Api;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Core.Credentials;

public class Account : IEquatable<Account>
{
  private string _id { get; set; }

  public string id
  {
    get
    {
      if (_id == null)
      {
        if (serverInfo == null || userInfo == null)
        {
          throw new SpeckleException("Incomplete account info: cannot generate id.");
        }

        _id = Crypt.Md5(userInfo.email + serverInfo.url, "X2");
      }
      return _id;
    }
    set => _id = value;
  }

  public string token { get; set; }

  public string refreshToken { get; set; }

  public bool isDefault { get; set; } = false;
  public bool isOnline { get; set; } = true;

  public ServerInfo serverInfo { get; set; }

  public UserInfo userInfo { get; set; }

  #region private methods

  private static string CleanURL(string server)
  {
    if (Uri.TryCreate(server, UriKind.Absolute, out Uri newUri))
    {
      server = newUri.Authority;
    }

    return server;
  }

  #endregion

  #region public methods

  public string GetHashedEmail()
  {
    string email = userInfo?.email ?? "unknown";
    return "@" + Crypt.Md5(email, "X2");
  }

  public string GetHashedServer()
  {
    string url = serverInfo?.url ?? AccountManager.DEFAULT_SERVER_URL;
    return Crypt.Md5(CleanURL(url), "X2");
  }

  public async Task<UserInfo> Validate()
  {
    return await AccountManager.GetUserInfo(token, serverInfo.url).ConfigureAwait(false);
  }

  public override string ToString()
  {
    return $"Account ({userInfo.email} | {serverInfo.url})";
  }

  public bool Equals(Account other)
  {
    return other is not null && other.userInfo.email == userInfo.email && other.serverInfo.url == serverInfo.url;
  }

  public override bool Equals(object obj)
  {
    return obj is Account acc && Equals(acc);
  }

  #endregion
}
