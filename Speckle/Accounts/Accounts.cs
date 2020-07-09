using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Speckle.Transports;

namespace Speckle
{

  public class Account
  {
    public string url { get; set; }

    public string token { get; private set; }

    public string refreshToken { get; private set; }

    public Account()
    {
    }

    private static SqlLiteObjectTransport AccountStorage = new SqlLiteObjectTransport(scope: "Accounts");

    // NOTE: These need to be coordinated with the server.
    private static string APPID = "connectors";
    private static string SECRET = "connectors";
    private static int PORT = 24707;

    /// <summary>
    /// Adds a new account at the specified server via the standard authentication flow. 
    /// </summary>
    /// <param name="serverUrl"></param>
    /// <returns></returns>
    public static async Task<Account> AddNewAccount(string serverUrl)
    {
      Uri serverUri;
      var uriOk = Uri.TryCreate(serverUrl, UriKind.Absolute, out serverUri);

      if (!uriOk)
        throw new Exception("Invalid url provided.");

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

      var accessCode = req.Url.Query.Split('=')[1];
      var cp = accessCode;

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
          throw new Exception($"Failed to get api token for {serverUrl}");
        }

        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenExchangeResponse>(await _response.Content.ReadAsStringAsync());

        var account = new Account()
        {
          refreshToken = response.refreshToken,
          token = response.token,
          url = serverUrl
        };

        var existing = AccountStorage.GetObject(serverUrl);

        if (existing != null)
        {
          AccountStorage.DeleteObject(serverUrl);
        }

        AccountStorage.SaveObjectSync(serverUrl, Newtonsoft.Json.JsonConvert.SerializeObject(account));

        return account;
      }
    }

    private class TokenExchangeResponse
    {
      public string token { get; set; }
      public string refreshToken { get; set; }
    }
  }
}
