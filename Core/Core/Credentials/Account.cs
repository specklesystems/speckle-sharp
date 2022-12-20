using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;

namespace Speckle.Core.Credentials
{

  public class Account : IEquatable<Account>
  {
    private string _id { get; set; } = null;
    public string id
    {
      get
      {
        if (_id == null)
        {

          if (serverInfo == null || userInfo == null)
            throw new SpeckleException("Incomplete account info: cannot generate id.", level: Sentry.SentryLevel.Error);
          _id = Speckle.Core.Models.Utilities.hashString(userInfo.email + serverInfo.url, Models.Utilities.HashingFuctions.MD5).ToUpper();

        }
        return _id;
      }
      set
      {
        _id = value;
      }
    }
    public string token { get; set; }

    public string refreshToken { get; set; }

    public bool isDefault { get; set; } = false;
    public bool isOnline { get; set; } = true;

    public ServerInfo serverInfo { get; set; }

    public UserInfo userInfo { get; set; }

    public Account() { }

    #region public methods

    public string GetHashedEmail()
    {

      string email = userInfo?.email ?? "unknown";
      return "@" + Hash(email);
    }

    public string GetHashedServer()
    {
      string url = serverInfo?.url ?? "https://speckle.xyz/";
      return Hash(CleanURL(url));
    }

    public async Task<UserInfo> Validate()
    {
      using var httpClient = Http.GetHttpProxyClient();

      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

      using var gqlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(serverInfo.url), "/graphql") }, new NewtonsoftJsonSerializer(), httpClient);

      var request = new GraphQLRequest
      {
        Query = @" query { user { name email id company } }"
      };

      var response = await gqlClient.SendQueryAsync<UserInfoResponse>(request);

      if (response.Errors != null)
        return null;

      return response.Data.user;
    }

    public bool Equals(Account other)
    {
      return other.userInfo.email == userInfo.email && other.serverInfo.url == serverInfo.url;
    }

    public override string ToString()
    {
      return $"Account ({userInfo.email} | {serverInfo.url})";
    }

    #endregion

    #region private methods
    private static string CleanURL(string server)
    {
      Uri NewUri;

      if (Uri.TryCreate(server, UriKind.Absolute, out NewUri))
      {
        server = NewUri.Authority;
      }
      return server;
    }

    private static string Hash(string input)
    {

      using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
      {
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input.ToLowerInvariant());
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
          sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString();
      }

    }

    #endregion
  }
}
