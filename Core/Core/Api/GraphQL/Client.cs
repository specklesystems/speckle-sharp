using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Client.Http;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;
using GraphQL;
using Polly;
using Serilog;
using Polly.Contrib.WaitAndRetry;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Speckle.Core.Api
{
  public partial class Client : IDisposable
  {
    public string ServerUrl => Account.serverInfo.url;

    public string ApiToken => Account.token;

    public System.Version ServerVersion { get; set; }

    [JsonIgnore]
    public Account Account { get; set; }

    HttpClient HttpClient { get; set; }

    public GraphQLHttpClient GQLClient { get; set; }

    public object UploadValues(string v1, string v2, NameValueCollection user_1)
    {
      throw new NotImplementedException();
    }

    public Client() { }

    public Client(Account account)
    {
      if (account == null)
        throw new SpeckleException($"Provided account is null.");

      Account = account;

      HttpClient = Http.GetHttpProxyClient();

      if (account.token.ToLowerInvariant().Contains("bearer"))
      {
        HttpClient.DefaultRequestHeaders.Add("Authorization", account.token);
      }
      else
      {
        HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {account.token}");
      }

      HttpClient.DefaultRequestHeaders.Add("apollographql-client-name", Setup.HostApplication);
      HttpClient.DefaultRequestHeaders.Add(
        "apollographql-client-version",
        Assembly.GetExecutingAssembly().GetName().Version.ToString()
      );

      GQLClient = new GraphQLHttpClient(
        new GraphQLHttpClientOptions
        {
          EndPoint = new Uri(new Uri(account.serverInfo.url), "/graphql"),
          UseWebSocketForQueriesAndMutations = false,
          ConfigureWebSocketConnectionInitPayload = (opts) =>
          {
            return new { Authorization = $"Bearer {account.token}" };
          },
          OnWebsocketConnected = OnWebSocketConnect,
        },
        new NewtonsoftJsonSerializer(),
        HttpClient
      );

      GQLClient.WebSocketReceiveErrors.Subscribe(e =>
      {
        if (e is WebSocketException we)
          Console.WriteLine(
            $"WebSocketException: {we.Message} (WebSocketError {we.WebSocketErrorCode}, ErrorCode {we.ErrorCode}, NativeErrorCode {we.NativeErrorCode}"
          );
        else
          Console.WriteLine($"Exception in websocket receive stream: {e.ToString()}");
      });
    }

    public Task OnWebSocketConnect(GraphQLHttpClient client)
    {
      return Task.CompletedTask;
    }

    public void Dispose()
    {
      try
      {
        UserStreamAddedSubscription?.Dispose();
        UserStreamRemovedSubscription?.Dispose();
        StreamUpdatedSubscription?.Dispose();
        BranchCreatedSubscription?.Dispose();
        BranchUpdatedSubscription?.Dispose();
        BranchDeletedSubscription?.Dispose();
        CommitCreatedSubscription?.Dispose();
        CommitUpdatedSubscription?.Dispose();
        CommitDeletedSubscription?.Dispose();
        CommentActivitySubscription?.Dispose();
        GQLClient?.Dispose();
      }
      catch { }
    }

    internal async Task<T> ExecuteWithResiliencePolicies<T>(Func<Task<T>> func)
    {
      // TODO: handle these in the HttpClient factory with a custom RequestHandler class
      // 408 Request Timeout
      // 425 Too Early
      // 429 Too Many Requests
      // 500 Internal Server Error
      // 502 Bad Gateway
      // 503 Service Unavailable
      // 504 Gateway Timeout

      var delay = Backoff.DecorrelatedJitterBackoffV2(
        medianFirstRetryDelay: TimeSpan.FromSeconds(1),
        retryCount: 5
      );
      var graphqlRetry = Policy
        .Handle<SpeckleGraphQLInternalErrorException<T>>()
        .WaitAndRetryAsync(
          delay,
          onRetry: (ex, retryCount, context) =>
          {
            Log.ForContext("exceptionMessage", ex.Message)
              .ForContext("context", context)
              .Warning(
                "The previous attempt at executing function to get {resultType} failed with {exceptionType}. Retrying for the {retryCount}th time.",
                nameof(T),
                ex.GetType().Name,
                retryCount
              );
          }
        );

      return await graphqlRetry.ExecuteAsync(func);
    }

    internal async Task<T> ExecuteGraphQLRequest<T>(
      GraphQLRequest request,
      CancellationToken? cancellationToken
    )
    {
      try
      {
        return await ExecuteWithResiliencePolicies<T>(async () =>
        {
          var result = await GQLClient
            .SendMutationAsync<T>(request, cancellationToken ?? CancellationToken.None)
            .ConfigureAwait(false);
          MaybeThrowFromGraphQLErrors(request, result);
          return result.Data;
        });
      }
      // we catch forbidden to rethrow, making sure its not logged.
      catch (SpeckleGraphQLForbiddenException<T>)
      {
        throw;
      }
      // anything else related to graphql gets logged
      catch (SpeckleGraphQLException<T> gqlException)
      {
        Log.ForContext("errorMessages", gqlException.ErrorMessages.ToList())
          .ForContext("extensions", gqlException.Extensions)
          .ForContext("exceptionMessage", gqlException.Message)
          .Error(
            gqlException,
            "Execution of the graphql request {resultType} failed with {graphqlExceptionType}",
            nameof(T),
            gqlException.GetType().Name
          );
        throw;
      }
    }

    internal void MaybeThrowFromGraphQLErrors<T>(
      GraphQLRequest request,
      GraphQLResponse<T> response
    )
    {
      // The errors reflect the Apollo server v2 API, which is deprecated. It is bound to change,
      // once we migrate to a newer version.
      var errors = response.Errors;
      if (errors != null && errors.Any())
      {
        var errorMessages = errors.Select(e => e.Message);
        if (
          errors.Any(
            e =>
              e.Extensions != null
              && (
                e.Extensions.Contains(new KeyValuePair<string, object>("code", "FORBIDDEN"))
                || e.Extensions.Contains(
                  new KeyValuePair<string, object>("code", "UNAUTHENTICATED")
                )
              )
          )
        )
          throw new SpeckleGraphQLForbiddenException<T>(request, response);

        if (
          errors.Any(
            e =>
              e.Extensions != null
              && e.Extensions.Contains(
                new KeyValuePair<string, object>("code", "INTERNAL_SERVER_ERROR")
              )
          )
        )
          throw new SpeckleGraphQLInternalErrorException<T>(request, response);

        throw new SpeckleGraphQLException<T>("Request failed with errors", request, response);
      }
    }
  }
}
