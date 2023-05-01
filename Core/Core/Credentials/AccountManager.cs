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
  private static SQLiteTransport AccountStorage = new(scope: "Accounts");
  private static bool _isAddingAccount;
  private static SQLiteTransport AccountAddLockStorage = new(scope: "AccountAddFlow");

  /// <summary>
  /// Gets the basic information about a server.
  /// </summary>
  /// <param name="server">Server URL</param>
  /// <returns></returns>
  public static async Task<ServerInfo> GetServerInfo(string server)
  {
    using var httpClient = Http.GetHttpProxyClient();

    using var gqlClient = new GraphQLHttpClient(
      new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql") },
      new NewtonsoftJsonSerializer(),
      httpClient
    );

    var request = new GraphQLRequest { Query = @" query { serverInfo { name company } }" };

    var response = await gqlClient.SendQueryAsync<ServerInfoResponse>(request).ConfigureAwait(false);

    if (response.Errors != null)
      return null;

    response.Data.serverInfo.url = server;

    return response.Data.serverInfo;
  }

  /// <summary>
  /// Gets basic user information given a token and a server.
  /// </summary>
  /// <param name="token"></param>
  /// <param name="server">Server URL</param>
  /// <returns></returns>
  public static async Task<UserInfo> GetUserInfo(string token, string server)
  {
    using var httpClient = Http.GetHttpProxyClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

    using var gqlClient = new GraphQLHttpClient(
      new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql") },
      new NewtonsoftJsonSerializer(),
      httpClient
    );

    var request = new GraphQLRequest { Query = @" query { user { name email id company } }" };

    var response = await gqlClient.SendQueryAsync<UserInfoResponse>(request).ConfigureAwait(false);

    if (response.Errors != null)
      return null;

    return response.Data.user;
  }

  /// <summary>
  /// Gets basic user and server information given a token and a server.
  /// </summary>
  /// <param name="token"></param>
  /// <param name="server">Server URL</param>
  /// <returns></returns>
  private static async Task<UserServerInfoResponse> GetUserServerInfo(string token, string server)
  {
    try
    {
      using var httpClient = Http.GetHttpProxyClient();
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

      using var client = new GraphQLHttpClient(
        new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql") },
        new NewtonsoftJsonSerializer(),
        httpClient
      );

      var request = new GraphQLRequest
      {
        Query =
          @"query { user { id name email company avatar streams { totalCount } commits { totalCount } } serverInfo { name company adminContact description version} }"
      };

      var res = await client.SendQueryAsync<UserServerInfoResponse>(request).ConfigureAwait(false);

      if (res.Errors != null && res.Errors.Any())
        throw new SpeckleException(res.Errors[0].Message, res.Errors);

      return res.Data;
    }
    catch (Exception e)
    {
      throw new SpeckleException(e.Message, e);
    }
  }

  /// <summary>
  /// The Default Server URL for authentication, can be overridden by placing a file with the alternatrive url in the Speckle folder or with an ENV_VAR
  /// </summary>
  public static string GetDefaultServerUrl()
  {
    var defaultServerUrl = "https://speckle.xyz";
    var customServerUrl = "";

    // first mechanism, check for local file
    var customServerFile = Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "server");
    if (File.Exists(customServerFile))
      customServerUrl = File.ReadAllText(customServerFile);

    // second mechanism, check ENV VAR
    var customServerEnvVar = Environment.GetEnvironmentVariable("SPECKLE_SERVER");
    if (!string.IsNullOrEmpty(customServerEnvVar))
      customServerUrl = customServerEnvVar;

    if (!string.IsNullOrEmpty(customServerUrl))
    {
      Uri url = null;
      Uri.TryCreate(customServerUrl, UriKind.Absolute, out url);
      if (url != null)
        defaultServerUrl = customServerUrl.TrimEnd('/');
    }

    return defaultServerUrl;
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
        SpeckleLog.Logger.Information("No Speckle accounts found. Visit the Speckle web app to create one.");
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

    //prevent invalid account from slipping out
    var invalidAccounts = sqlAccounts.Where(x => x.userInfo == null || x.serverInfo == null);
    foreach (var acc in invalidAccounts)
      RemoveAccount(acc.id);

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
    var accountsDir = SpecklePathProvider.AccountsFolderPath;
    if (!Directory.Exists(accountsDir))
      return accounts;
    var files = Directory.GetFiles(accountsDir, "*.json", SearchOption.AllDirectories);
    foreach (var file in files)
      try
      {
        var json = File.ReadAllText(file);
        var account = JsonConvert.DeserializeObject<Account>(json);

        if (
          !string.IsNullOrEmpty(account.token)
          && !string.IsNullOrEmpty(account.userInfo.id)
          && !string.IsNullOrEmpty(account.userInfo.email)
          && !string.IsNullOrEmpty(account.userInfo.name)
          && !string.IsNullOrEmpty(account.serverInfo.url)
          && !string.IsNullOrEmpty(account.serverInfo.name)
        )
          accounts.Add(account);
      }
      catch
      {
        //ignore it
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
        if (userServerInfo?.user == null || userServerInfo.serverInfo == null)
        {
          var tokenResponse = await GetRefreshedToken(account.refreshToken, url).ConfigureAwait(false);
          userServerInfo = await GetUserServerInfo(tokenResponse.token, url).ConfigureAwait(false);

          if (userServerInfo?.user == null || userServerInfo.serverInfo == null)
            throw new SpeckleException("Could not refresh token");

          account.token = tokenResponse.token;
          account.refreshToken = tokenResponse.refreshToken;
        }

        account.isOnline = true;
        account.userInfo = userServerInfo.user;
        account.serverInfo = userServerInfo.serverInfo;
        account.serverInfo.url = url;
      }
      catch (Exception ex)
      {
        account.isOnline = false;
      }

      AccountStorage.UpdateObject(account.id, JsonConvert.SerializeObject(account));
    }
  }

  /// <summary>
  /// Removes an account
  /// </summary>
  /// <param name="id">ID of the account to remove</param>
  public static void RemoveAccount(string id)
  {
    //TODO: reset default account
    AccountStorage.DeleteObject(id);

    var accounts = GetAccounts();

    if (accounts.Any() && !accounts.Any(x => x.isDefault))
      ChangeDefaultAccount(accounts.First().id);
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
        account.isDefault = false;
      else
        account.isDefault = true;

      AccountStorage.UpdateObject(account.id, JsonConvert.SerializeObject(account));
    }
  }

  private static string _ensureCorrectServerUrl(string server)
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

  private static void _ensureGetAccessCodeFlowIsSupported()
  {
    if (!HttpListener.IsSupported)
    {
      SpeckleLog.Logger.Error("HttpListener not supported");
      throw new Exception("Your operating system is not supported");
    }
  }

  private static async Task<string> _getAccessCode(string server, string challenge, TimeSpan timeout)
  {
    _ensureGetAccessCodeFlowIsSupported();

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
      var message = "";
      if (accessCode != null)
        message = "Success!<br/><br/>You can close this window now.<script>window.close();</script>";
      else
        message = "Oups, something went wrong...!";

      var responseString =
        $"<HTML><BODY Style='background: linear-gradient(to top right, #ffffff, #c8e8ff); font-family: Roboto, sans-serif; font-size: 2rem; font-weight: 500; text-align: center;'><br/>{message}</BODY></HTML>";
      byte[] buffer = Encoding.UTF8.GetBytes(responseString);
      response.ContentLength64 = buffer.Length;
      Stream output = response.OutputStream;
      output.Write(buffer, 0, buffer.Length);
      output.Close();
      SpeckleLog.Logger.Debug("Processed finished processing the access code.");
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

  private static async Task<Account> _createAccount(string accessCode, string challenge, string server)
  {
    try
    {
      var tokenResponse = await GetToken(accessCode, challenge, server).ConfigureAwait(false);
      var userResponse = await GetUserServerInfo(tokenResponse.token, server).ConfigureAwait(false);

      var account = new Account
      {
        token = tokenResponse.token,
        refreshToken = tokenResponse.refreshToken,
        isDefault = GetAccounts().Count() == 0,
        serverInfo = userResponse.serverInfo,
        userInfo = userResponse.user
      };
      SpeckleLog.Logger.Information("Successfully created account for {serverUrl}", server);
      account.serverInfo.url = server;

      return account;
    }
    catch (Exception ex)
    {
      throw new SpeckleAccountManagerException("Failed to create account from access code and challenge", ex);
    }
  }

  private static void _tryLockAccountAddFlow(TimeSpan timespan)
  {
    // use a static variable to quickly
    // prevent launching this flow multiple times
    if (_isAddingAccount)
      // this should probably throw with an error message
      throw new SpeckleAccountFlowLockedException("The account add flow is already launched.");

    // this uses the SQLite transport to store locks
    var lockIds = AccountAddLockStorage.GetAllObjects().OrderByDescending(d => d).ToList();
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
    AccountAddLockStorage.SaveObjectSync(lockId, lockId);
    _isAddingAccount = true;
  }

  private static void _unlockAccountAddFlow()
  {
    _isAddingAccount = false;
    // make sure all old locks are removed
    foreach (var id in AccountAddLockStorage.GetAllObjects())
      AccountAddLockStorage.DeleteObject(id);
  }

  /// <summary>
  /// Adds an account by propting the user to log in via a web flow
  /// </summary>
  /// <param name="server">Server to use to add the account, if not provied the default Server will be used</param>
  /// <returns></returns>
  public static async Task AddAccount(string server = "")
  {
    SpeckleLog.Logger.Debug("Starting to add account for {serverUrl}", server);

    server = _ensureCorrectServerUrl(server);

    // locking for 1 minute
    var timeout = TimeSpan.FromMinutes(1);
    // this is not part of the try finally block
    // we do not want to clean up the existing locks
    _tryLockAccountAddFlow(timeout);
    var challenge = GenerateChallenge();

    var accessCode = "";

    try
    {
      accessCode = await _getAccessCode(server, challenge, timeout).ConfigureAwait(false);
      if (string.IsNullOrEmpty(accessCode))
        throw new SpeckleAccountManagerException("Access code is invalid");

      var account = await _createAccount(accessCode, challenge, server).ConfigureAwait(false);

      //if the account already exists it will not be added again
      AccountStorage.SaveObject(account.id, JsonConvert.SerializeObject(account));
      SpeckleLog.Logger.Debug("Finished adding account {accountId} for {serverUrl}", account.id, server);
    }
    catch (SpeckleAccountManagerException ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to add account: {exceptionMessage}", ex.Message);
      // rethrowing any known errors
      throw;
    }
    catch (Exception ex)
    {
      SpeckleLog.Logger.Fatal(ex, "Failed to add account: {exceptionMessage}", ex.Message);
      throw new SpeckleAccountManagerException($"Failed to add account: {ex.Message}", ex);
    }
    finally
    {
      _unlockAccountAddFlow();
    }
  }

  private static async Task<TokenExchangeResponse> GetToken(string accessCode, string challenge, string server)
  {
    try
    {
      ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
      var client = Http.GetHttpProxyClient();

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
    catch (Exception e)
    {
      throw new SpeckleException(e.Message, e);
    }
  }

  private static async Task<TokenExchangeResponse> GetRefreshedToken(string refreshToken, string server)
  {
    try
    {
      ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
      var client = Http.GetHttpProxyClient();

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
    catch (Exception e)
    {
      throw new SpeckleException(e.Message, e);
    }
  }

  private static string GenerateChallenge()
  {
    using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
    {
      byte[] challengeData = new byte[32];
      rng.GetBytes(challengeData);

      //escaped chars like % do not play nice with the server
      return Regex.Replace(Convert.ToBase64String(challengeData), @"[^\w\.@-]", "");
    }
  }
}
