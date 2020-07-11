
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Speckle.Transports;
using Newtonsoft.Json;
using System.Collections.Generic;
using GraphQL;
using GraphQL.Client.Serializer.Newtonsoft;
using GraphQL.Client.Http;

namespace Speckle.Credentials
{

  public static class AccountManager
  {

    private static SqlLiteObjectTransport AccountStorage = new SqlLiteObjectTransport(scope: "Accounts");

    // NOTE: These need to be coordinated with the server.
    private static string APPID = "connectors";
    private static string SECRET = "connectors";
    private static int PORT = 24707;

    /// <summary>
    /// Adds a new account at the specified server via the standard authentication flow.
    /// <para>Note: this will work only in desktop environments that have a browser, and it depends on user interaction.</para>
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static async Task<Account> AddNewAccount(string serverUrl)
    {
      Uri serverUri;
      var uriOk = Uri.TryCreate(serverUrl, UriKind.Absolute, out serverUri);

      if (!uriOk)
        throw new Exception("Invalid url provided.");

      var _serverInfo = await GetServerInfo(serverUrl);

      if (_serverInfo == null)
        throw new Exception($"Could not get the server information for {serverUrl}");

      var challenge = Speckle.Models.Utilities.hashString(DateTime.UtcNow.ToString());
      var url = $"{serverUri}auth?appId={APPID}&challenge={challenge}";

      // Cross platform browser open. Sigh.
      try
      {
        Process.Start(url);
      }
      catch
      {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          url = url.Replace("&", "^&");
          Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
          Process.Start("open", url);
        }
        else
        {
          throw;
        }
      }

      var listener = new HttpListener();
      listener.Prefixes.Add($"http://localhost:{PORT}/");

      listener.Start(); // this blocks until a request is received.

      var ctx = listener.GetContext();
      var req = ctx.Request;

      listener.Stop();

      if (req.Url.Query.Contains("success=false"))
      {
        throw new Exception($"Permission denied/failed ({serverUrl}).");
      }

      var queryPieces = req.Url.Query.Split('=');

      if(queryPieces.Length < 2 || !queryPieces[0].Contains("access_code"))
      {
        throw new Exception($"Invalid access token response ({req.Url.Query}).");
      }

      var accessCode = queryPieces[1];

      // exchange access code for token
      using (var client = new HttpClient())
      {
        var request = new HttpRequestMessage()
        {
          RequestUri = new Uri($"{serverUri}auth/token"),
          Method = HttpMethod.Post
        };

        request.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new { appId = APPID, appSecret = SECRET, accessCode = accessCode, challenge = challenge }));

        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var _response = await client.SendAsync(request);

        try
        {
          _response.EnsureSuccessStatusCode();
        }
        catch
        {
          throw new Exception($"Failed to get api token for {serverUrl}. Response status: {_response.StatusCode}.");
        }

        var _tokens = JsonConvert.DeserializeObject<TokenExchangeResponse>(await _response.Content.ReadAsStringAsync());

        var _userInfo = await GetUserInfo(_tokens.token, serverUrl);

        var account = new Account()
        {
          refreshToken = _tokens.refreshToken,
          token = _tokens.token,
          serverInfo = _serverInfo,
          userInfo = _userInfo
        };

        //var existing = AccountStorage.GetObject(serverUrl);

        //if (existing != null)
        //{
        //  AccountStorage.DeleteObject(serverUrl);
        //}

        //AccountStorage.SaveObjectSync(serverUrl, Newtonsoft.Json.JsonConvert.SerializeObject(account));

        return account;
      }
    }

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
    /// Gets basic user information.
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
    /// Gets a specific account from this environment.
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static Account GetLocalAccount(string serverUrl)
    {
      var _acc = AccountStorage.GetObject(serverUrl);

      if (_acc == null) throw new Exception($"No account found for {serverUrl}.");

      return JsonConvert.DeserializeObject<Account>(_acc);
    }

    /// <summary>
    /// Deletes an account from this environment.
    /// </summary>
    /// <param name="serverUrl"></param>
    public static void DeleteLocalAccount(string serverUrl)
    {
      AccountStorage.DeleteObject(serverUrl);
    }

    /// <summary>
    /// Gets all the accounts present in this environment.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Account> GetAllLocalAccounts()
    {
      var _accs = AccountStorage.GetAllObjects();
      foreach (var _acc in _accs)
        yield return JsonConvert.DeserializeObject<Account>(_acc);
    }

  }
}
