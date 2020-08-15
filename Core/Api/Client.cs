using System;
using System.Net.Http;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Speckle.Core.Credentials;

namespace Speckle.Core.Api
{
  public partial class Client
  {
    public string ServerUrl { get => Account.serverInfo.url; }

    public string ApiToken { get => Account.token; }

    public string AccountId { get; set;}

    [JsonIgnore]
    public Account Account { get; set; }

    HttpClient HttpClient { get; set; }

    GraphQLHttpClient GQLClient { get; set; }

    public Client() { }

    public Client(Account account)
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

  }
}
