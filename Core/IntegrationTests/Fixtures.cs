using System.Net.Mime;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace TestsIntegration
{
  [SetUpFixture]
  public class SetUp
  {
    [OneTimeSetUp]
    public void BeforeAll()
    {
      SpeckleLog.Initialize(
        "Core",
        "Testing",
        new SpeckleLogConfiguration(
          Serilog.Events.LogEventLevel.Debug,
          logToConsole: true,
          logToFile: false,
          logToSeq: false
        )
      );
      SpeckleLog.Logger.Information("Initialized logger for testing");
    }
  }

  public static class Fixtures
  {
    public static readonly ServerInfo Server = new ServerInfo
    {
      url = "http://localhost:3000",
      name = "Docker Server"
    };

    public static async Task<Account> SeedUser()
    {
      var seed = Guid.NewGuid().ToString().ToLower();
      var user = new Dictionary<string, string>();
      user["email"] = $"{seed.Substring(0, 7)}@acme.com";
      user["password"] = "12ABC3456789DEF0GHO";
      user["name"] = $"{seed.Substring(0, 5)} Name";

      var httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = false });
      httpClient.BaseAddress = new Uri(Server.url);

      string redirectUrl;
      try
      {
        var response = await httpClient.PostAsync(
          "/auth/local/register?challenge=challengingchallenge",
          // $"{Server.url}/auth/local/register?challenge=challengingchallenge",
          new StringContent(
            JsonConvert.SerializeObject(user),
            Encoding.UTF8,
            MediaTypeNames.Application.Json
          )
        );
        redirectUrl = response.Headers.Location.AbsoluteUri;
      }
      catch (Exception e)
      {
        throw new Exception($"Cannot seed user on the server {Server.url}", e);
      }

      var uri = new Uri(redirectUrl);
      var query = HttpUtility.ParseQueryString(uri.Query);
      
      var accessCode = query["access_code"] ?? throw new Exception("Redirect Uri has no 'access_code'.");
      var tokenBody = new Dictionary<string, string>()
      {
        ["accessCode"] = accessCode,
        ["appId"] = "spklwebapp",
        ["appSecret"] = "spklwebapp",
        ["challenge"] = "challengingchallenge"
      };

      var tokenResponse = await httpClient.PostAsync(
        "/auth/token",
        new StringContent(
          JsonConvert.SerializeObject(tokenBody),
          Encoding.UTF8,
          MediaTypeNames.Application.Json
        )
      );
      var deserialised = JsonConvert.DeserializeObject<Dictionary<string, string>>(
        await tokenResponse.Content.ReadAsStringAsync()
      );

      var acc = new Account
      {
        token = deserialised["token"],
        userInfo = new UserInfo
        {
          id = user["name"],
          email = user["email"],
          name = user["name"]
        },
        serverInfo = Server
      };
      var client = new Client(acc);

      var user1 = await client.ActiveUserGet();
      acc.userInfo.id = user1.id;
      return acc;
    }

    public static Base GenerateSimpleObject()
    {
      var @base = new Base();
      @base["foo"] = "foo";
      @base["bar"] = "bar";
      @base["baz"] = "baz";
      @base["now"] = DateTime.Now.ToString();

      return @base;
    }

    public static Base GenerateNestedObject()
    {
      var @base = new Base();
      @base["foo"] = "foo";
      @base["bar"] = "bar";
      @base["@baz"] = new Base();
      ((Base)@base["@baz"])["mux"] = "mux";
      ((Base)@base["@baz"])["qux"] = "qux";

      return @base;
    }

    public static Blob[] GenerateThreeBlobs() =>
      new Blob[]
      {
        GenerateBlob("blob 1 data"),
        GenerateBlob("blob 2 data"),
        GenerateBlob("blob 3 data"),
      };

    private static Blob GenerateBlob(string content)
    {
      var filePath = Path.GetTempFileName();
      File.WriteAllText(filePath, content);
      return new Blob(filePath);
    }
  }

  public class UserIdResponse
  {
    public string userId { get; set; }
    public string apiToken { get; set; }
  }
}
