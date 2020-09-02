using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Sentry.Protocol;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.Core.Api
{
  public partial class Client : IDisposable
  {
    public string ServerUrl { get => Account.serverInfo.url; }

    public string ApiToken { get => Account.token; }

    public string AccountId { get; set; }

    [JsonIgnore]
    public Account Account { get; set; }

    HttpClient HttpClient { get; set; }

    public GraphQLHttpClient GQLClient { get; set; }

    public Client() { }

    public Client(Account account)
    {
      if (account == null)
        Log.CaptureAndThrow(new SpeckleException($"Provided account is null."));

      Account = account;
      AccountId = account.id;

      HttpClient = new HttpClient();

      if (account.token.ToLowerInvariant().Contains("bearer"))
      {
        HttpClient.DefaultRequestHeaders.Add("Authorization", account.token);
      }
      else
      {
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {account.token}");
      }

      GQLClient = new GraphQLHttpClient(
        new GraphQLHttpClientOptions
        {
          EndPoint = new Uri(new Uri(account.serverInfo.url), "/graphql"),
          UseWebSocketForQueriesAndMutations = false,
          OnWebsocketConnected = OnWebSocketConnect,
          ConfigureWebsocketOptions = (opts) =>
          {
            opts.SetRequestHeader("Authorization", $"Bearer {account.token}");
          },

        },
        new NewtonsoftJsonSerializer(),
        HttpClient);

      GQLClient.WebSocketReceiveErrors.Subscribe(e =>
      {
        if (e is WebSocketException we)
          Console.WriteLine($"WebSocketException: {we.Message} (WebSocketError {we.WebSocketErrorCode}, ErrorCode {we.ErrorCode}, NativeErrorCode {we.NativeErrorCode}");
        else
          Console.WriteLine($"Exception in websocket receive stream: {e.ToString()}");
      });

    }

    public Task OnWebSocketConnect(GraphQLHttpClient client)
    {
      Console.WriteLine("Websocket is open");
      return Task.CompletedTask;
    }

    public void Dispose()
    {
      UserStreamAddedSubscription.Dispose();
      StreamRemovedSubscription.Dispose();
      StreamUpdatedSubscription.Dispose();
      GQLClient.Dispose();
    }


  }
}
