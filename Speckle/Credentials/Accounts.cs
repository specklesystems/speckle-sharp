using System;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Net.Http;

namespace Speckle.Credentials
{

  public class Account : IEquatable<Account>
  {

    public string id
    {
      get
      {
        if (serverInfo == null || userInfo == null) throw new Exception("Incomplete account info: cannot generate id.");
        return Speckle.Models.Utilities.hashString(serverInfo.url + userInfo.email);
      }
    }

    public string token { get; set; }

    public string refreshToken { get; set; }

    public bool isDefault { get; set; } = false;

    public ServerInfo serverInfo { get; set; }

    public UserInfo userInfo { get; set; }

    public Account() { }

    public async Task<UserInfo> Validate()
    {
      using var httpClient = new HttpClient();

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

    public async Task RotateToken()
    {
      throw new NotImplementedException();
    }

    public bool Equals(Account other)
    {
      return other.userInfo.email == userInfo.email && other.serverInfo.url == serverInfo.url;
    }
  }
}
