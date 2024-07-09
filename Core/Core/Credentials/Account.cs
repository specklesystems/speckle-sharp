#nullable disable
using System;
using System.Threading.Tasks;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Helpers;

namespace Speckle.Core.Credentials;

public class Account : IEquatable<Account>
{
  private string _id;

  /// <remarks>
  /// The account id is unique to user and server url.
  /// </remarks>
  /// <exception cref="InvalidOperationException">Account object invalid: missing required info</exception>
  public string id
  {
    get
    {
      if (_id == null)
      {
        if (serverInfo == null || userInfo == null)
        {
          throw new InvalidOperationException("Incomplete account info: cannot generate id.");
        }

        _id = Crypt.Md5(userInfo.email + serverInfo.url, "X2");
      }
      return _id;
    }
    set => _id = value;
  }

  public string token { get; set; }

  public string refreshToken { get; set; }

  public bool isDefault { get; set; }
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
    Uri server = new(serverInfo.url);
    return await AccountManager.GetUserInfo(token, server).ConfigureAwait(false);
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

  /// <summary>
  /// Retrieves the local identifier for the current user.
  /// </summary>
  /// <returns>
  /// Returns a <see cref="Uri"/> object representing the local identifier for the current user.
  /// The local identifier is created by appending the user ID as a query parameter to the server URL.
  /// </returns>
  /// <remarks>
  /// Notice that the generated Uri is not intended to be used as a functioning Uri, but rather as a
  /// unique identifier for a specific account in a local environment. The format of the Uri, containing a query parameter with the user ID,
  /// serves this specific purpose. Therefore, it should not be used for forming network requests or
  /// expecting it to lead to an actual webpage. The primary intent of this Uri is for unique identification in a Uri format.
  /// </remarks>
  /// <example>
  ///   This sample shows how to call the GetLocalIdentifier method.
  ///   <code>
  ///     Uri localIdentifier = GetLocalIdentifier();
  ///     Console.WriteLine(localIdentifier);
  ///   </code>
  ///   For a fictional `User ID: 123` and `Server: https://speckle.xyz`, the output might look like this:
  ///   <code>
  ///     https://speckle.xyz?id=123
  ///   </code>
  /// </example>
  internal Uri GetLocalIdentifier() => new($"{serverInfo.url}?id={userInfo.id}");
}
