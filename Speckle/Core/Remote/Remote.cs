using System;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Speckle.Credentials;

namespace Speckle.Core
{
  public partial class Remote
  {
    public string ServerUrl { get => Account.serverInfo.url; }

    public string ApiToken { get => Account.token; }

    public string StreamId { get; set; }

    public string AccountId { get; set;}

    [JsonIgnore]
    public Account Account { get; set; }

    HttpClient HttpClient { get; set; }

    GraphQLHttpClient GQLClient { get; set; }

    public Remote() { }

    public Remote(Account account)
    {
      if (account == null)
        throw new Exception("Provided account is null.");

      Account = account;
      AccountId = account.id;

      HttpClient = new HttpClient();
      HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {account.token}");

      GQLClient = new GraphQLHttpClient(
        new GraphQLHttpClientOptions
        {
          EndPoint = new Uri(new Uri(account.serverInfo.url), "/graphql"),
        },
        new NewtonsoftJsonSerializer(),
        HttpClient); 
    }

    public async Task<string> InitializeNewStream()
    {
      StreamId = await StreamCreate(new GqlModels.StreamInput { name = "Test Stream", description = "Really this is just a test stream." });

      return StreamId;
    }

  }
}
