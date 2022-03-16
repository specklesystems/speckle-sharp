
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    /// <summary>
    /// The Default Server URL for authentication
    /// </summary>
    //private const string ServerUrl = "http://localhost:3000";
    private const string ServerUrl = "https://speckle.xyz";

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

    public static void LogOut()
    {
      AccountStorage.DeleteObject(AccountManager.GetDefaultAccount().id);
    }


    /// <summary>
    /// Propts the user to log in via a web flow and stores the account in the local SQLite db
    /// </summary>
    /// <returns></returns>
    public static async Task LogIn()
    {

      var accessCode = "";
      var challenge = GenerateChallenge();
      Process.Start(new ProcessStartInfo($"{ServerUrl}/authn/verify/sdm/{challenge}") { UseShellExecute = true });

      await Task.Run(() =>
      {
        if (!HttpListener.IsSupported)
        {
          Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
          return;
        }


        // Create a listener.
        HttpListener listener = new HttpListener();

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


      });

      var tokenResponse = (await GetToken(accessCode, challenge));

      var userResponse = await GetUser(tokenResponse.token);

      var account = new Account()
      {
        token = tokenResponse.token,
        refreshToken = tokenResponse.refreshToken,
        isDefault = true, // assuming there are no other accounts!
        serverInfo = userResponse.serverInfo,
        userInfo = userResponse.user
      };

      account.serverInfo.url = ServerUrl;

      AccountStorage.SaveObject(account.id, JsonConvert.SerializeObject(account));
    }

    private static async Task<TokenResponse> GetToken(string accessCode, string challenge)
    {
      try
      {
        var httpWebRequest = (HttpWebRequest)WebRequest.Create($"{ServerUrl}/auth/token");
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
          return JsonConvert.DeserializeObject<TokenResponse>(result);
        }


      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }


    }

    private static async Task<UserResponse> GetUser(string token)
    {

      try
      {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");


        var client = new GraphQLHttpClient(
         new GraphQLHttpClientOptions
         {
           EndPoint = new Uri(new Uri(ServerUrl), "/graphql"),
           UseWebSocketForQueriesAndMutations = false,
           ConfigureWebSocketConnectionInitPayload = (opts) => { return new { Authorization = $"Bearer {token}" }; },
         },
         new NewtonsoftJsonSerializer(),
         httpClient);

        var request = new GraphQLRequest
        {
          Query = @"query { user { id name email company } serverInfo { name company adminContact description version} }"
        };

        var res = await client.SendQueryAsync<UserResponse>(request).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data;
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

    internal class TokenResponse
    {
      public string token { get; set; }
      public string refreshToken { get; set; }
    }

    internal class UserResponse
    {
      public UserInfo user { get; set; }
      public ServerInfo serverInfo { get; set; }
    }


  }



}
