using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using static Speckle.Core.Models.Utilities;

namespace Speckle.Core.Credentials;

public class Account : IEquatable<Account>
{
  public Account() { }

  private string _id { get; set; }

  public string id
  {
    get
    {
      if (_id == null)
      {
        if (serverInfo == null || userInfo == null)
          throw new SpeckleException("Incomplete account info: cannot generate id.");
        _id = hashString(userInfo.email + serverInfo.url, HashingFuctions.MD5).ToUpper();
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

  public bool Equals(Account other)
  {
    if (ReferenceEquals(null, other))
      return false;
    if (ReferenceEquals(this, other))
      return true;
    return Equals(serverInfo.url, other.serverInfo.url)
      && Equals(userInfo.email, other.userInfo.email);
  }

  private static string CleanURL(string server)
  {
    Uri NewUri;

    if (Uri.TryCreate(server, UriKind.Absolute, out NewUri))
      server = NewUri.Authority;
    return server;
  }

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
    using var httpClient = Http.GetHttpProxyClient();

    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

    using var gqlClient = new GraphQLHttpClient(
      new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(serverInfo.url), "/graphql") },
      new NewtonsoftJsonSerializer(),
      httpClient
    );

    var request = new GraphQLRequest { Query = @" query { user { name email id company } }" };

    var response = await gqlClient.SendQueryAsync<UserInfoResponse>(request).ConfigureAwait(false);

    if (response.Errors != null)
      return null;

    return response.Data.user;
  }

  public override string ToString()
  {
    return $"Account ({userInfo.email} | {serverInfo.url})";
  }

  public override bool Equals(object obj)
  {
    return obj is Account acc && Equals(acc);
  }

  public override int GetHashCode()
  {
    unchecked
    {
      return ((serverInfo.url != null ? serverInfo.url.GetHashCode() : 0) * 397)
        ^ (userInfo.email != null ? userInfo.email.GetHashCode() : 0);
    }
  }

  public static bool operator ==(Account left, Account right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(Account left, Account right)
  {
    return !Equals(left, right);
  }
}
