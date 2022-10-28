using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using System.Text;
using System.Net.Mime;

namespace TestsIntegration
{
  public static class Fixtures
  {
    public static readonly ServerInfo Server = new ServerInfo { url = "http://localhost:3000", name = "Docker Server" };

    public static async Task<Account> SeedUser()
    {
      var seed = Guid.NewGuid().ToString().ToLower();
      var user = new Dictionary<string, string>();
      user["email"] = $"{seed.Substring(0, 7)}@acme.com";
      user["password"] = "12ABC3456789DEF0GHO";
      user["name"] = $"{seed.Substring(0, 5)} Name";

      var httpClient = new HttpClient(new HttpClientHandler()
      {
        AllowAutoRedirect = false
      });
      httpClient.BaseAddress = new Uri(Server.url);

      string redirectUrl;
      try
      {
        var response = await httpClient.PostAsync(
          "/auth/local/register?challenge=challengingchallenge",
          // $"{Server.url}/auth/local/register?challenge=challengingchallenge",
          new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, MediaTypeNames.Application.Json)
        );
        redirectUrl = response.Headers.Location.AbsoluteUri;
      }
      catch (Exception e)
      {
        throw new Exception($"Cannot seed user on the server {Server.url}", e);
      }

      var accessCode = redirectUrl.Split("?access_code=")[1];
      var tokenBody = new Dictionary<string, string>()
      {
        ["accessCode"] = accessCode,
        ["appId"] = "spklwebapp",
        ["appSecret"] = "spklwebapp",
        ["challenge"] = "challengingchallenge"
      };

      var tokenResponse = await httpClient.PostAsync(
        "/auth/token",
        new StringContent(JsonConvert.SerializeObject(tokenBody), Encoding.UTF8, MediaTypeNames.Application.Json)
      );
      var deserialised = JsonConvert.DeserializeObject<Dictionary<string, string>>(
        await tokenResponse.Content.ReadAsStringAsync()
      );

      var acc = new Account { 
        token = deserialised["token"], 
        userInfo = new UserInfo { id = user["name"], email = user["email"], name = user["name"] }, 
        serverInfo = Server 
      };
      var client = new Client(acc);

      var user1 = await client.ActiveUserGet();
      acc.userInfo.id = user1.id;

      return acc;
    }
  }

  public class UserIdResponse
  {
    public string userId { get; set; }
    public string apiToken { get; set; }
  }
}
