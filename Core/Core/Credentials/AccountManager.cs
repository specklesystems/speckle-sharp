
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
using System.IO;

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
          Log.CaptureException(new SpeckleException("No Speckle accounts found. Visit the Speckle web app to create one."), level: Sentry.SentryLevel.Info);
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
      var sqlAccounts = AccountStorage.GetAllObjects().Select(x => JsonConvert.DeserializeObject<Account>(x));
      var localAccounts = GetLocalAccounts();

      var allAccounts = sqlAccounts.Concat(localAccounts);

      return allAccounts;
    }

    /// <summary>
    /// Gets the local accounts
    /// These are accounts not handled by Manager and are stored in json format in a local directory
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<Account> GetLocalAccounts()
    {
      var accounts = new List<Account>();
      var accountsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle", "Accounts");
      if (!Directory.Exists(accountsDir))
      {
        return accounts;
      }
      var files = Directory.GetFiles(accountsDir, "*.json", SearchOption.AllDirectories);
      foreach (var file in files)
      {
        try
        {
          var json = File.ReadAllText(file);
          var account = JsonConvert.DeserializeObject<Account>(json);

          if (
            !string.IsNullOrEmpty(account.token) &&
            !string.IsNullOrEmpty(account.userInfo.id) &&
            !string.IsNullOrEmpty(account.userInfo.email) &&
            !string.IsNullOrEmpty(account.userInfo.name) &&
            !string.IsNullOrEmpty(account.serverInfo.url) &&
            !string.IsNullOrEmpty(account.serverInfo.name)
            )
            accounts.Add(account);
        }
        catch
        { //ignore it
        }
      }
      return accounts;
    }

  }
}
