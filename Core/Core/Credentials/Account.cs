using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Core.Credentials;

#pragma warning disable CS0659 CA1067 //TODO: Disabled to prevent GetHashCode from being added by the cleanup.

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
          throw new SpeckleException("Incomplete account info: cannot generate id.");
        _id = Utilities.HashString(userInfo.email + serverInfo.url, Utilities.HashingFunctions.MD5).ToUpper();
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
    Uri NewUri;

    if (Uri.TryCreate(server, UriKind.Absolute, out NewUri))
      server = NewUri.Authority;
    return server;
  }

  #endregion

  #region public methods

  public string GetHashedEmail()
  {
    string email = userInfo?.email ?? "unknown";
    return "@" + Crypt.Hash(email);
  }

  public string GetHashedServer()
  {
    string url = serverInfo?.url ?? "https://speckle.xyz/";
    return Crypt.Hash(CleanURL(url));
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
#pragma warning restore CS0659 CA1067
