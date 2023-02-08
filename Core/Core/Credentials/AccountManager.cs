﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Serilog;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Serializer;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

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
    /// <param name="server">Server URL</param>
    /// <returns></returns>
    public static async Task<ServerInfo> GetServerInfo(string server)
    {
      using var httpClient = Http.GetHttpProxyClient();

      using var gqlClient = new GraphQLHttpClient(
        new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(server), "/graphql") },
        new NewtonsoftJsonSerializer(),
        httpClient
      );

      var request = new GraphQLRequest { Query = @" query { serverInfo { name company } }" };

      var response = await gqlClient.SendQueryAsync<ServerInfoResponse>(request);

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
        new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(server), "/graphql") },
        new NewtonsoftJsonSerializer(),
        httpClient
      );

      var request = new GraphQLRequest { Query = @" query { user { name email id company } }" };

      var response = await gqlClient.SendQueryAsync<UserInfoResponse>(request);

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
        var httpClient = Http.GetHttpProxyClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var client = new GraphQLHttpClient(
          new GraphQLHttpClientOptions { EndPoint = new Uri(new Uri(server), "/graphql"), },
          new NewtonsoftJsonSerializer(),
          httpClient
        );

        var request = new GraphQLRequest
        {
          Query =
            @"query { user { id name email company avatar streams { totalCount } commits { totalCount } } serverInfo { name company adminContact description version} }"
        };

        var res = await client
          .SendQueryAsync<UserServerInfoResponse>(request)
          .ConfigureAwait(false);

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
          defaultServerUrl = customServerUrl.TrimEnd(new[] { '/' });
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
          Log.Information("No Speckle accounts found. Visit the Speckle web app to create one.");
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
      var sqlAccounts = AccountStorage
        .GetAllObjects()
        .Select(x => JsonConvert.DeserializeObject<Account>(x));
      var localAccounts = GetLocalAccounts();

      //prevent invalid account from slipping out
      var invalidAccounts = sqlAccounts.Where(x => x.userInfo == null || x.serverInfo == null);
      foreach (var acc in invalidAccounts)
      {
        RemoveAccount(acc.id);
      }

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
        { //ignore it
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
          var userServerInfo = await GetUserServerInfo(account.token, url);

          //the token has expired
          //TODO: once we get a token expired exception from the server use that instead
          if (
            userServerInfo == null
            || userServerInfo.user == null
            || userServerInfo.serverInfo == null
          )
          {
            var tokenResponse = await GetRefreshedToken(account.refreshToken, url);
            userServerInfo = await GetUserServerInfo(tokenResponse.token, url);

            if (
              userServerInfo == null
              || userServerInfo.user == null
              || userServerInfo.serverInfo == null
            )
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

        AccountStorage.UpdateObject(account.id, JsonConvert.SerializeObject(account));
      }
    }

    /// <summary>
    /// Adds an account by propting the user to log in via a web flow
    /// </summary>
    /// <param name="server">Server to use to add the account, if not provied the default Server will be used</param>
    /// <returns></returns>
    public static async Task AddAccount(string server = "")
    {
      Log.Debug("Starting to add account for {serverUrl}", server);
      server = server.TrimEnd(new[] { '/' });

      if (string.IsNullOrEmpty(server)) {
        server = GetDefaultServerUrl();
        Log.Debug("Changed server url to the default usl {serverUrl}", server);
      }

      var accessCode = "";
      var challenge = GenerateChallenge();
      Log.Debug("Starting auth process for {server}/authn/verify/sca/{challenge}", server, challenge);
      Process.Start(
        new ProcessStartInfo($"{server}/authn/verify/sca/{challenge}") { UseShellExecute = true }
      );

      HttpListener listener = new HttpListener();

      //does nothing?
      var timeout = TimeSpan.FromMinutes(2);
      //listener.TimeoutManager.HeaderWait = timeout;
      //listener.TimeoutManager.EntityBody = timeout;
      //listener.TimeoutManager.IdleConnection = timeout;

      var task = Task.Run(() =>
      {
        try
        {
          if (!HttpListener.IsSupported)
          {
            Log.Warning(
              "Windows XP SP2 or Server 2003 is required to use the HttpListener class."
            );
            return;
          }

          listener = new HttpListener();
          var localUrl = "http://localhost:29363/";
          listener.Prefixes.Add(localUrl);

          listener.Start();
          Log.Debug("Listening for auth redirects on {localUrl}", localUrl);
          // Note: The GetContext method blocks while waiting for a request.
          HttpListenerContext context = listener.GetContext();
          HttpListenerRequest request = context.Request;
          HttpListenerResponse response = context.Response;

          accessCode = request.QueryString["access_code"];
          Log.Debug("Got access code {accessCode}", accessCode);
          var message = "";
          if (accessCode != null)
          {
            message =
              "Yay!<br/><br/>You can close this window now.<script>window.close();</script>";
          }
          else
          {
            message = "Oups, something went wrong...!";
          }

          var responseString =
            $"<HTML><BODY Style='background: linear-gradient(to top right, #ffffff, #c8e8ff); font-family: Roboto, sans-serif; font-size: 2rem; font-weight: 500; text-align: center;'><br/>{message}</BODY></HTML>";

          byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
          response.ContentLength64 = buffer.Length;
          System.IO.Stream output = response.OutputStream;
          output.Write(buffer, 0, buffer.Length);
          output.Close();
          Log.Debug("Processed finished processing the access code.");
          listener.Stop();
        }
        catch (Exception ex)
        { 
          Log.Error(ex, "Getting access code flow failed with {exceptionMessage}", ex.Message);
        }
      });

      //Timeout
      if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
      {
        Log.Information("Local auth flow completed within the timeout window. Access code is {accessCode}", accessCode);
        // task completed within timeout
      }
      else
      {
        // nada
        Log.Warning("Local auth flow failed to complete within the timeout window. Access code is {accessCode}", accessCode);
      }

      if (string.IsNullOrEmpty(accessCode))
        return;

      var tokenResponse = (await GetToken(accessCode, challenge, server));

      var userResponse = await GetUserServerInfo(tokenResponse.token, server);

      var account = new Account()
      {
        token = tokenResponse.token,
        refreshToken = tokenResponse.refreshToken,
        isDefault = GetAccounts().Count() == 0,
        serverInfo = userResponse.serverInfo,
        userInfo = userResponse.user
      };
      Log.Information("Successfully created account for {serverUrl}", server);
      account.serverInfo.url = server;

      //if the account already exists it will not be added again
      AccountStorage.SaveObject(account.id, JsonConvert.SerializeObject(account));
    }

    private static async Task<TokenExchangeResponse> GetToken(
      string accessCode,
      string challenge,
      string server
    )
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
          accessCode = accessCode,
          challenge = challenge,
        };

        var content = new StringContent(JsonConvert.SerializeObject(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.PostAsync($"{server}/auth/token", content);

        return JsonConvert.DeserializeObject<TokenExchangeResponse>(
          await response.Content.ReadAsStringAsync()
        );
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    private static async Task<TokenExchangeResponse> GetRefreshedToken(
      string refreshToken,
      string server
    )
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
          refreshToken = refreshToken
        };

        var content = new StringContent(JsonConvert.SerializeObject(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.PostAsync($"{server}/auth/token", content);

        return JsonConvert.DeserializeObject<TokenExchangeResponse>(
          await response.Content.ReadAsStringAsync()
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
}
