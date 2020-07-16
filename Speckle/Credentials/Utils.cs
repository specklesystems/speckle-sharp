
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
using System.Linq;

namespace Speckle.Credentials
{

  /// <summary>
  /// Manage accounts locally for desktop applications.
  /// </summary>
  public static class AccountManager
  {
    private static SqlLiteObjectTransport AccountStorage = new SqlLiteObjectTransport(scope: "Accounts");

    // NOTE: These need to be coordinated with the server.
    internal static string APPID = "connectors";
    internal static string SECRET = "connectors";
    internal static int PORT = 24707;

    /// <summary>
    /// Adds a new account at the specified server via the standard authentication flow. It will open the default browser and load the authentication endpoint of the given server, where the end user will need to authenticate and then grant permissions to your app.
    /// <para>Note: this method depends on user interaction. For use cases where this is not possible (e.g., server-side apps/scripts), set tokens otherwise (e.g., via environment variables).</para>
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static async Task<Account> Authenticate(string serverUrl)
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

        var payload = new {
          appId = APPID,
          appSecret = SECRET,
          accessCode,
          challenge
        };

        request.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload));

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

        UpdateOrSaveAccount(account);

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
    /// Gets this environment's default account if any.
    /// </summary>
    /// <returns>The default account or null.</returns>
    public static Account GetDefaultAccount()
    {
      return GetAccounts().Where(acc => acc.isDefault).FirstOrDefault();
    }

    /// <summary>
    /// Sets the default account.
    /// </summary>
    /// <param name="id"></param>
    public static void SetDefaultAccount(string id)
    {
      foreach(var acc in GetAccounts())
      {
        if(acc.id == id)
        {
          acc.isDefault = true;
        } else
        {
          acc.isDefault = false;
        }

        UpdateOrSaveAccount(acc);
      }
    }

    /// <summary>
    /// Creates or updates an account. <b>Note:</b> This method allows you to change any properties <i>besides</i> the serverInfo's url and userInfo's email. 
    /// </summary>
    /// <param name="account"></param>
    public static void UpdateOrSaveAccount(Account account)
    {
      AccountStorage.DeleteObject(account.id);
      AccountStorage.SaveObjectSync(account.id, JsonConvert.SerializeObject(account));
    }

    /// <summary>
    /// Deletes an account from this environment.
    /// </summary>
    /// <param name="id"></param>
    public static void DeleteLocalAccount(string id)
    {
      AccountStorage.DeleteObject(id);
    }

    /// <summary>
    /// Gets all the accounts present in this environment.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<Account> GetAccounts()
    {
      var _accs = AccountStorage.GetAllObjects();
      foreach (var _acc in _accs)
        yield return JsonConvert.DeserializeObject<Account>(_acc);
    }

  }
}
