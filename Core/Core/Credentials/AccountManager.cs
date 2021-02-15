
using Speckle.Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Speckle.Core.Transports;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Client.Http;
using System.Linq;
using Speckle.Core.Logging;
using Speckle.Core.Api.GraphQL.Serializer;

namespace Speckle.Core.Credentials
{

  /// <summary>
  /// Manage accounts locally for desktop applications.
  /// </summary>
  public static class AccountManager
  {
    private static SQLiteTransport AccountStorage = new SQLiteTransport(scope: "Accounts");

    /// <summary>
    /// Gets the basic information about a server. 
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static async Task<ServerInfo> GetServerInfo(string serverUrl)
    {
      using var httpClient = new HttpClient();

      using var gqlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(serverUrl), "/graphql") }, new NewtonsoftJsonSerializer(), httpClient);

      var request = new GraphQLRequest
      {
        Query = @" query { serverInfo { name company } }"
      };

      var response = await gqlClient.SendQueryAsync<ServerInfoResponse>(request);

      if (response.Errors != null)
        return null;

      response.Data.serverInfo.url = serverUrl;

      return response.Data.serverInfo;
    }

    /// <summary>
    /// Gets basic user information given a token and a server.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public static async Task<UserInfo> GetUserInfo(string token, string url)
    {
      using var httpClient = new HttpClient();

      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

      using var gqlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(url), "/graphql") }, new NewtonsoftJsonSerializer(), httpClient);

      var request = new GraphQLRequest
      {
        Query = @" query { user { name email id company } }"
      };

      var response = await gqlClient.SendQueryAsync<UserInfoResponse>(request);

      if (response.Errors != null)
        return null;

      return response.Data.user;
    }

    /// <summary>
    /// Gets all the accounts for a given server.
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static IEnumerable<Account> GetAccounts(string serverUrl)
    {
      return GetAccounts().Where(acc => acc.serverInfo.url == serverUrl);
    }

    /// <summary>
    /// Gets this environment's default account if any. If there is no default, the first found will be returned and set as default.
    /// </summary>
    /// <returns>The default account or null.</returns>
    public static Account GetDefaultAccount()
    {
      var defaultAccount = GetAccounts().Where(acc => acc.isDefault).FirstOrDefault();
      if (defaultAccount == null)
      {
        var firstAccount = GetAccounts().FirstOrDefault();
        if (firstAccount == null)
        {
          Log.CaptureException(new SpeckleException("No Speckle accounts found. Visit the Speckle web app to create one."), level: Sentry.Protocol.SentryLevel.Info);
        }
        return firstAccount;
      }
      return defaultAccount;
    }

    /// <summary>
    /// Gets all the accounts present in this environment.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Account> GetAccounts()
    {
      var _accs = AccountStorage.GetAllObjects();
      foreach (var _acc in _accs)
      {
        yield return JsonConvert.DeserializeObject<Account>(_acc);
      }
    }

  }
}
