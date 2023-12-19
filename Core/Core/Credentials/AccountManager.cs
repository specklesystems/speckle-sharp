using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;
using Stream = System.IO.Stream;

namespace Speckle.Core.Credentials;

/// <summary>
/// Manage accounts locally for desktop applications.
/// </summary>
public static class AccountManager
{
  public const string DEFAULT_SERVER_URL = "https://app.speckle.systems";

  private static readonly SQLiteTransport s_accountStorage = new(scope: "Accounts");
  private static bool s_isAddingAccount;
  private static readonly SQLiteTransport s_accountAddLockStorage = new(scope: "AccountAddFlow");

  /// <summary>
  /// Gets the basic information about a server.
  /// </summary>
  /// <param name="server">Server URL</param>
  /// <returns></returns>
  public static async Task<ServerInfo?> GetServerInfo(string server)
  {
    using var httpClient = Http.GetHttpProxyClient();

    using var gqlClient = new GraphQLHttpClient(
      new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql") },
      new NewtonsoftJsonSerializer(),
      httpClient
    );

    var request = new GraphQLRequest { Query = @" query { serverInfo { name company } }" };

    var response = await gqlClient.SendQueryAsync<ServerInfoResponse>(request).ConfigureAwait(false);

    if (response.Errors is not null)
    {
      return null;
    }

    response.Data.serverInfo.url = server;

    return response.Data.serverInfo;
  }

  /// <summary>
  /// Gets basic user information given a token and a server.
  /// </summary>
  /// <param name="token"></param>
  /// <param name="server">Server URL</param>
  /// <returns></returns>
  public static async Task<UserInfo?> GetUserInfo(string token, string server)
  {
    using var httpClient = Http.GetHttpProxyClient();
    Http.AddAuthHeader(httpClient, token);

    using var gqlClient = new GraphQLHttpClient(
      new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql") },
      new NewtonsoftJsonSerializer(),
      httpClient
    );

    var request = new GraphQLRequest { Query = @" query { activeUser { name email id company } }" };

    var response = await gqlClient.SendQueryAsync<ActiveUserResponse>(request).ConfigureAwait(false);

    if (response.Errors is not null)
    {
      return null;
    }

    return response.Data.activeUser;
  }

  /// <summary>
  /// Gets basic user and server information given a token and a server.
  /// </summary>
  /// <param name="token"></param>
  /// <param name="server">Server URL</param>
  /// <returns></returns>
  private static async Task<ActiveUserServerInfoResponse> GetUserServerInfo(string token, string server)
  {
    try
    {
      using var httpClient = Http.GetHttpProxyClient();
      Http.AddAuthHeader(httpClient, token);

      using var client = new GraphQLHttpClient(
        new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql") },
        new NewtonsoftJsonSerializer(),
        httpClient
      );

      var request = new GraphQLRequest
      {
        Query =
          @"query { activeUser { id name email company avatar streams { totalCount } commits { totalCount } } serverInfo { name company adminContact description version} }"
      };

      var res = await client.SendQueryAsync<ActiveUserServerInfoResponse>(request).ConfigureAwait(false);

      if (res.Errors is not null && res.Errors.Length != 0)
      {
        throw new SpeckleException(res.Errors[0].Message, res.Errors);
      }

      return res.Data;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw new SpeckleException(ex.Message, ex);
    }
  }

  /// <summary>
  /// The Default Server URL for authentication, can be overridden by placing a file with the alternatrive url in the Speckle folder or with an ENV_VAR
  /// </summary>
  public static string GetDefaultServerUrl()
  {
    var serverUrl = DEFAULT_SERVER_URL;
    var customServerUrl = "";

    // first mechanism, check for local file
    var customServerFile = Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "server");
    if (File.Exists(customServerFile))
    {
      customServerUrl = File.ReadAllText(customServerFile);
    }

    // second mechanism, check ENV VAR
    var customServerEnvVar = Environment.GetEnvironmentVariable("SPECKLE_SERVER");
    if (!string.IsNullOrEmpty(customServerEnvVar))
    {
      customServerUrl = customServerEnvVar;
    }

    if (!string.IsNullOrEmpty(customServerUrl))
    {
      Uri.TryCreate(customServerUrl, UriKind.Absolute, out Uri url);
      if (url != null)
      {
        serverUrl = customServerUrl.TrimEnd('/');
      }
    }

    return serverUrl;
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
  public static Account? GetDefaultAccount()
  {
    var defaultAccount = GetAccounts().FirstOrDefault(acc => acc.isDefault);
    if (defaultAccount != null)
    {
      return defaultAccount;
    }

    var firstAccount = GetAccounts().FirstOrDefault();
    if (firstAccount == null)
    {
      SpeckleLog.Logger.Information("No Speckle accounts found. Visit the Speckle web app to create one");
    }

    return firstAccount;
  }

  /// <summary>
  /// Gets all the accounts present in this environment.
  /// </summary>
  /// <remarks>This function does have potential side effects. Any invalid accounts found while enumerating will be removed</remarks>
  /// <returns>Un-enumerated enumerable of accounts</returns>
  public static IEnumerable<Account> GetAccounts()
  {
    static bool IsInvalid(Account ac) => ac.userInfo == null || ac.serverInfo == null;

    var sqlAccounts = s_accountStorage.GetAllObjects().Select(x => JsonConvert.DeserializeObject<Account>(x));
    var localAccounts = GetLocalAccounts();

    foreach (var acc in sqlAccounts)
    {
      if (IsInvalid(acc))
      {
        RemoveAccount(acc.id);
      }
      else
      {
        yield return acc;
      }
    }

    foreach (var acc in localAccounts)
    {
      yield return acc;
    }
  }

  /// <summary>
  /// Gets the local accounts
  /// These are accounts not handled by Manager and are stored in json format in a local directory
  /// </summary>
  /// <returns></returns>
  private static IList<Account> GetLocalAccounts()
  {
    var accountsDir = SpecklePathProvider.AccountsFolderPath;
    if (!Directory.Exists(accountsDir))
    {
      return Array.Empty<Account>();
    }

    var accounts = new List<Account>();
    string[] files = Directory.GetFiles(accountsDir, "*.json", SearchOption.AllDirectories);
    foreach (var file in files)
    {
      try
      {
        var json = File.ReadAllText(file);
        Account? account = JsonConvert.DeserializeObject<Account>(json);

        if (
          account is not null
          && !string.IsNullOrEmpty(account.token)
          && !string.IsNullOrEmpty(account.userInfo.id)
          && !string.IsNullOrEmpty(account.userInfo.email)
          && !string.IsNullOrEmpty(account.userInfo.name)
          && !string.IsNullOrEmpty(account.serverInfo.url)
          && !string.IsNullOrEmpty(account.serverInfo.name)
        )
        {
          accounts.Add(account);
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        SpeckleLog.Logger.Warning(ex, "Failed to load json account at {filePath}", file);
      }
    }

    return accounts;
  }

  /// <summary>
  /// Refetches user and server info for each account
  /// </summary>
  /// <returns></returns>
  public static async Task UpdateAccounts()
  {
    foreach (var account in GetAccounts())
    {
      var url = account.serverInfo.url;

      try
      {
        var userServerInfo = await GetUserServerInfo(account.token, url).ConfigureAwait(false);

        //the token has expired
        //TODO: once we get a token expired exception from the server use that instead
        if (userServerInfo?.activeUser == null || userServerInfo.serverInfo == null)
        {
          var tokenResponse = await GetRefreshedToken(account.refreshToken, url).ConfigureAwait(false);
          userServerInfo = await GetUserServerInfo(tokenResponse.token, url).ConfigureAwait(false);

          if (userServerInfo?.activeUser == null || userServerInfo.serverInfo == null)
          {
            throw new SpeckleException("Could not refresh token");
          }

          account.token = tokenResponse.token;
          account.refreshToken = tokenResponse.refreshToken;
        }

        account.isOnline = true;
        account.userInfo = userServerInfo.activeUser;
        account.serverInfo = userServerInfo.serverInfo;
        account.serverInfo.url = url;
        account.serverInfo.frontend2 = await IsFrontend2Server(url).ConfigureAwait(false);
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        account.isOnline = false;
      }

      s_accountStorage.UpdateObject(account.id, JsonConvert.SerializeObject(account));
    }
  }

  /// <summary>
  /// Removes an account
  /// </summary>
  /// <param name="id">ID of the account to remove</param>
  public static void RemoveAccount(string id)
  {
    //TODO: reset default account
    s_accountStorage.DeleteObject(id);

    var accounts = GetAccounts();
    //BUG: Clearly this is a bug bug bug!
    if (accounts.Any() && !accounts.Any(x => x.isDefault))
    {
      ChangeDefaultAccount(accounts.First().id);
    }
  }

  /// <summary>
  /// Changes the default account
  /// </summary>
  /// <param name="id"></param>
  public static void ChangeDefaultAccount(string id)
  {
    foreach (var account in GetAccounts())
    {
      if (account.id != id)
      {
        account.isDefault = false;
      }
      else
      {
        account.isDefault = true;
      }

      s_accountStorage.UpdateObject(account.id, JsonConvert.SerializeObject(account));
    }
  }

  private static string EnsureCorrectServerUrl(string server)
  {
    var localUrl = server;
    if (string.IsNullOrEmpty(localUrl))
    {
      localUrl = GetDefaultServerUrl();
      SpeckleLog.Logger.Debug(
        "The provided server url was null or empty. Changed to the default url {serverUrl}",
        localUrl
      );
    }
    return localUrl.TrimEnd('/');
  }

  private static void EnsureGetAccessCodeFlowIsSupported()
  {
    if (!HttpListener.IsSupported)
    {
      SpeckleLog.Logger.Error("HttpListener not supported");
      throw new Exception("Your operating system is not supported");
    }
  }

  private static async Task<string> GetAccessCode(string server, string challenge, TimeSpan timeout)
  {
    EnsureGetAccessCodeFlowIsSupported();

    SpeckleLog.Logger.Debug("Starting auth process for {server}/authn/verify/sca/{challenge}", server, challenge);

    var accessCode = "";

    Process.Start(new ProcessStartInfo($"{server}/authn/verify/sca/{challenge}") { UseShellExecute = true });

    var task = Task.Run(() =>
    {
      using var listener = new HttpListener();
      var localUrl = "http://localhost:29363/";
      listener.Prefixes.Add(localUrl);
      listener.Start();
      SpeckleLog.Logger.Debug("Listening for auth redirects on {localUrl}", localUrl);
      // Note: The GetContext method blocks while waiting for a request.
      HttpListenerContext context = listener.GetContext();
      HttpListenerRequest request = context.Request;
      HttpListenerResponse response = context.Response;

      accessCode = request.QueryString["access_code"];
      SpeckleLog.Logger.Debug("Got access code {accessCode}", accessCode);

      string message =
        accessCode != null
          ? "Success!<br/><br/>You can close this window now.<script>window.close();</script>"
          : "Oups, something went wrong...!";

      var responseString =
        $"<HTML><BODY Style='background: linear-gradient(to top right, #ffffff, #c8e8ff); font-family: Roboto, sans-serif; font-size: 2rem; font-weight: 500; text-align: center;'><br/>{message}</BODY></HTML>";
      byte[] buffer = Encoding.UTF8.GetBytes(responseString);
      response.ContentLength64 = buffer.Length;
      Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      SpeckleLog.Logger.Debug("Processed finished processing the access code");
      listener.Stop();
      listener.Close();
    });

    var completedTask = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);

    // this is means the task timed out
    if (completedTask != task)
    {
      SpeckleLog.Logger.Warning(
        "Local auth flow failed to complete within the timeout window. Access code is {accessCode}",
        accessCode
      );
      throw new Exception("Local auth flow failed to complete within the timeout window");
    }

    if (task.IsFaulted)
    {
      SpeckleLog.Logger.Error(
        task.Exception,
        "Getting access code flow failed with {exceptionMessage}",
        task.Exception.Message
      );
      throw new Exception($"Auth flow failed: {task.Exception.Message}", task.Exception);
    }

    // task completed within timeout
    SpeckleLog.Logger.Information(
      "Local auth flow completed successfully within the timeout window. Access code is {accessCode}",
      accessCode
    );
    return accessCode;
  }

  private static async Task<Account> CreateAccount(string accessCode, string challenge, string server)
  {
    try
    {
      var tokenResponse = await GetToken(accessCode, challenge, server).ConfigureAwait(false);
      var userResponse = await GetUserServerInfo(tokenResponse.token, server).ConfigureAwait(false);

      var account = new Account
      {
        token = tokenResponse.token,
        refreshToken = tokenResponse.refreshToken,
        isDefault = !GetAccounts().Any(),
        serverInfo = userResponse.serverInfo,
        userInfo = userResponse.activeUser
      };
      SpeckleLog.Logger.Information("Successfully created account for {serverUrl}", server);
      account.serverInfo.url = server;

      return account;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw new SpeckleAccountManagerException("Failed to create account from access code and challenge", ex);
    }
  }

  private static void TryLockAccountAddFlow(TimeSpan timespan)
  {
    // use a static variable to quickly
    // prevent launching this flow multiple times
    if (s_isAddingAccount)
    {
      // this should probably throw with an error message
      throw new SpeckleAccountFlowLockedException("The account add flow is already launched.");
    }

    // this uses the SQLite transport to store locks
    var lockIds = s_accountAddLockStorage.GetAllObjects().OrderByDescending(d => d).ToList();
    var now = DateTime.Now;
    foreach (var l in lockIds)
    {
      var lockArray = l.Split('@');
      var lockName = lockArray.Length == 2 ? lockArray[0] : "the other app";
      var lockTime =
        lockArray.Length == 2
          ? DateTime.ParseExact(lockArray[1], "o", null)
          : DateTime.ParseExact(lockArray[0], "o", null);

      if (lockTime > now)
      {
        var lockString = string.Format("{0:mm} minutes {0:ss} seconds", lockTime - now);
        throw new SpeckleAccountFlowLockedException(
          $"The account add flow was already started in {lockName}, retry in {lockString}"
        );
      }
    }

    var lockId = Setup.HostApplication + "@" + DateTime.Now.Add(timespan).ToString("o");

    // using the lock release time as an id and value
    // for ease of deletion and retrieval
    s_accountAddLockStorage.SaveObjectSync(lockId, lockId);
    s_isAddingAccount = true;
  }

  private static void UnlockAccountAddFlow()
  {
    s_isAddingAccount = false;
    // make sure all old locks are removed
    foreach (var id in s_accountAddLockStorage.GetAllObjects())
    {
      s_accountAddLockStorage.DeleteObject(id);
    }
  }

  /// <summary>
  /// Adds an account by propting the user to log in via a web flow
  /// </summary>
  /// <param name="server">Server to use to add the account, if not provied the default Server will be used</param>
  /// <returns></returns>
  public static async Task AddAccount(string server = "")
  {
    SpeckleLog.Logger.Debug("Starting to add account for {serverUrl}", server);

    server = EnsureCorrectServerUrl(server);

    // locking for 1 minute
    var timeout = TimeSpan.FromMinutes(1);
    // this is not part of the try finally block
    // we do not want to clean up the existing locks
    TryLockAccountAddFlow(timeout);
    var challenge = GenerateChallenge();

    try
    {
      string accessCode = await GetAccessCode(server, challenge, timeout).ConfigureAwait(false);
      if (string.IsNullOrEmpty(accessCode))
      {
        throw new SpeckleAccountManagerException("Access code is invalid");
      }

      var account = await CreateAccount(accessCode, challenge, server).ConfigureAwait(false);

      //if the account already exists it will not be added again
      s_accountStorage.SaveObject(account.id, JsonConvert.SerializeObject(account));
      SpeckleLog.Logger.Debug("Finished adding account {accountId} for {serverUrl}", account.id, server);
    }
    catch (SpeckleAccountManagerException ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to add account: {exceptionMessage}", ex.Message);
      // rethrowing any known errors
      throw;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to add account: {exceptionMessage}", ex.Message);
      throw new SpeckleAccountManagerException($"Failed to add account: {ex.Message}", ex);
    }
    finally
    {
      UnlockAccountAddFlow();
    }
  }

  private static async Task<TokenExchangeResponse> GetToken(string accessCode, string challenge, string server)
  {
    try
    {
      using var client = Http.GetHttpProxyClient();

      var body = new
      {
        appId = "sca",
        appSecret = "sca",
        accessCode,
        challenge
      };

      using var content = new StringContent(JsonConvert.SerializeObject(body));
      content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var response = await client.PostAsync($"{server}/auth/token", content).ConfigureAwait(false);

      return JsonConvert.DeserializeObject<TokenExchangeResponse>(
        await response.Content.ReadAsStringAsync().ConfigureAwait(false)
      );
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw new SpeckleException($"Failed to get authentication token from {server}", ex);
    }
  }

  private static async Task<TokenExchangeResponse> GetRefreshedToken(string refreshToken, string server)
  {
    try
    {
      using var client = Http.GetHttpProxyClient();

      var body = new
      {
        appId = "sca",
        appSecret = "sca",
        refreshToken
      };

      using var content = new StringContent(JsonConvert.SerializeObject(body));
      content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
      var response = await client.PostAsync($"{server}/auth/token", content).ConfigureAwait(false);

      return JsonConvert.DeserializeObject<TokenExchangeResponse>(
        await response.Content.ReadAsStringAsync().ConfigureAwait(false)
      );
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw new SpeckleException($"Failed to get refreshed token from {server}", ex);
    }
  }

  private static async Task<bool> IsFrontend2Server(string server)
  {
    try
    {
      using var client = Http.GetHttpProxyClient();
      var response = await client.GetAsync(server).ConfigureAwait(false);

      if (response.Headers.TryGetValues("x-speckle-frontend-2", out IEnumerable<string> values))
      {
        string? first = values.FirstOrDefault();
        if (first is not null && bool.Parse(first))
        {
          return true;
        }
      }

      return false;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      return false;
    }
  }

  private static string GenerateChallenge()
  {
    using RNGCryptoServiceProvider rng = new();
    byte[] challengeData = new byte[32];
    rng.GetBytes(challengeData);

    //escaped chars like % do not play nice with the server
    return Regex.Replace(Convert.ToBase64String(challengeData), @"[^\w\.@-]", "");
  }
}
