
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using Speckle.Core.Api.GraphQL.Serializer;
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
      using var httpClient = new HttpClient();

      using var gqlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(server), "/graphql") }, new NewtonsoftJsonSerializer(), httpClient);

      var request = new GraphQLRequest
      {
        Query = @" query { serverInfo { name company } }"
      };

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
      using var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

      using var gqlClient = new GraphQLHttpClient(new GraphQLHttpClientOptions() { EndPoint = new Uri(new Uri(server), "/graphql") }, new NewtonsoftJsonSerializer(), httpClient);

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
    /// Gets basic user and server information given a token and a server.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="server">Server URL</param>
    /// <returns></returns>
    private static async Task<UserServerInfoResponse> GetUserServerInfo(string token, string server)
    {

      try
      {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


        var client = new GraphQLHttpClient(
         new GraphQLHttpClientOptions
         {
           EndPoint = new Uri(new Uri(server), "/graphql"),
         },
         new NewtonsoftJsonSerializer(),
         httpClient);

        var request = new GraphQLRequest
        {
          Query = @"query { user { id name email company avatar} serverInfo { name company adminContact description version} }"
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
    /// The Default Server URL for authentication, can be overridden by placing a file with the alternatrive url in the Speckle folder
    /// </summary>
    public static string GetDefaultServerUrl()
    {
      var defaultServerUrl = "https://speckle.xyz";
      var local = Environment.SpecialFolder.ApplicationData;
      var system = Environment.SpecialFolder.CommonApplicationData;

      var folder = Assembly.GetAssembly(typeof(AccountManager)).Location.Contains("ProgramData") ? system : local;

      var customServerFile = Path.Combine(Environment.GetFolderPath(folder), "Speckle", "server");
      if (File.Exists(customServerFile))
      {
        var customUrl = File.ReadAllText(customServerFile);
        Uri url = null;
        Uri.TryCreate(customUrl, UriKind.Absolute, out url);
        if (url != null)
          defaultServerUrl = customUrl.TrimEnd(new[] { '/' });
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

    /// <summary>
    /// Refetches user and server info for each account
    /// </summary>
    /// <returns></returns>
    public static async Task UpdateAccounts()
    {
      foreach (var account in GetAccounts())
      {
        var url = account.serverInfo.url;
        var userServerInfo = await GetUserServerInfo(account.token, url);

        //prevent corrupting existing accounts
        if (userServerInfo == null || userServerInfo.user == null || userServerInfo.serverInfo == null)
          continue;

        account.userInfo = userServerInfo.user;
        account.serverInfo = userServerInfo.serverInfo;
        account.serverInfo.url = url;

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
    }


    /// <summary>
    /// Adds an account by propting the user to log in via a web flow
    /// </summary>
    /// <param name="server">Server to use to add the account, if not provied the default Server will be used</param>
    /// <returns></returns>
    public static async Task AddAccount(string server = "")
    {
      server = server.TrimEnd(new[] { '/' });

      if (string.IsNullOrEmpty(server))
        server = GetDefaultServerUrl();

      var accessCode = "";
      var challenge = GenerateChallenge();
      Process.Start(new ProcessStartInfo($"{server}/authn/verify/sca/{challenge}") { UseShellExecute = true });

      HttpListener listener = new HttpListener();

      //does nothing?
      var timeout = TimeSpan.FromMinutes(2);
      listener.TimeoutManager.HeaderWait = timeout;
      listener.TimeoutManager.EntityBody = timeout;
      listener.TimeoutManager.IdleConnection = timeout;

      var task = Task.Run(() =>
      {
        try
        {
          if (!HttpListener.IsSupported)
          {
            Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            return;
          }


          listener = new HttpListener();
          listener.Prefixes.Add("http://localhost:29363/");

          listener.Start();
          Console.WriteLine("Listening...");
          // Note: The GetContext method blocks while waiting for a request.
          HttpListenerContext context = listener.GetContext();
          HttpListenerRequest request = context.Request;
          HttpListenerResponse response = context.Response;

          accessCode = request.QueryString["access_code"];
          var message = "";
          if (accessCode != null)
          {
            message = "Yay!<br/><br/>You can close this window now.<script>window.close();</script>";
          }
          else
          {
            message = "Oups, something went wrong...!";
          }

          var responseString = $"<HTML><BODY Style='background: linear-gradient(to top right, #ffffff, #c8e8ff); font-family: Roboto, sans-serif; font-size: 2rem; font-weight: 500; text-align: center;'><br/>{message}</BODY></HTML>";

          byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
          response.ContentLength64 = buffer.Length;
          System.IO.Stream output = response.OutputStream;
          output.Write(buffer, 0, buffer.Length);
          output.Close();
          listener.Stop();

        }
        catch (Exception ex)
        {

        }
      });

      //Timeout
      if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
      {
        // task completed within timeout
      }
      else
      {
        // nada
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

      account.serverInfo.url = server;

      //if the account already exists it will not be added again
      AccountStorage.SaveObject(account.id, JsonConvert.SerializeObject(account));
    }

    private static async Task<TokenExchangeResponse> GetToken(string accessCode, string challenge, string server)
    {
      try
      {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create($"{server}/auth/token");
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

        var body = new
        {
          appId = "sca",
          appSecret = "sca",
          accessCode = accessCode,
          challenge = challenge,
        };

        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
          string json = JsonConvert.SerializeObject(body);

          streamWriter.Write(json);
          streamWriter.Flush();
          streamWriter.Close();
        }

        var httpResponse = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(httpWebRequest.BeginGetResponse, httpWebRequest.EndGetResponse, null);

        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        {
          var result = streamReader.ReadToEnd();
          return JsonConvert.DeserializeObject<TokenExchangeResponse>(result);
        }


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
