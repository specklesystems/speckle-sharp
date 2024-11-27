using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Integration;

[SetUpFixture]
public class SetUp
{
  [OneTimeSetUp]
  public void BeforeAll()
  {
    SpeckleLog.Initialize("Core", "Testing", new SpeckleLogConfiguration(logToFile: false, logToSeq: false));
    SpeckleLog.Logger.Information("Initialized logger for testing");
  }
}

public static class Fixtures
{
  public static readonly ServerInfo Server = new() { url = "http://localhost:3000", name = "Docker Server" };

  public static Client Unauthed => new Client(new Account { serverInfo = Server, userInfo = new UserInfo() });

  public static async Task<Client> SeedUserWithClient()
  {
    return new Client(await SeedUser());
  }

  public static async Task<string> CreateVersion(Client client, string projectId, string branchName)
  {
    using ServerTransport remote = new(client.Account, projectId);
    var objectId = await Operations.Send(new() { applicationId = "ASDF" }, remote, false);
    CommitCreateInput input =
      new()
      {
        branchName = branchName,
        message = "test version",
        objectId = objectId,
        streamId = projectId
      };
    return await client.Version.Create(input);
  }

  public static async Task<Account> SeedUser()
  {
    var seed = Guid.NewGuid().ToString().ToLower();
    Dictionary<string, string> user =
      new()
      {
        ["email"] = $"{seed.Substring(0, 7)}@example.com",
        ["password"] = "12ABC3456789DEF0GHO",
        ["name"] = $"{seed.Substring(0, 5)} Name"
      };

    using var httpClient = new HttpClient(
      new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true }
    );

    httpClient.BaseAddress = new Uri(Server.url);

    string redirectUrl;
    try
    {
      var response = await httpClient.PostAsync(
        "/auth/local/register?challenge=challengingchallenge",
        // $"{Server.url}/auth/local/register?challenge=challengingchallenge",
        new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, MediaTypeNames.Application.Json)
      );
      redirectUrl = response.Headers.Location!.AbsoluteUri;
    }
    catch (Exception e)
    {
      throw new Exception($"Cannot seed user on the server {Server.url}", e);
    }

    Uri uri = new(redirectUrl);
    var query = HttpUtility.ParseQueryString(uri.Query);

    string accessCode = query["access_code"] ?? throw new Exception("Redirect Uri has no 'access_code'.");
    Dictionary<string, string> tokenBody =
      new()
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

    var acc = new Account
    {
      token = deserialised["token"]!,
      userInfo = new UserInfo
      {
        id = user["name"],
        email = user["email"],
        name = user["name"]
      },
      serverInfo = Server
    };

    var user1 = await AccountManager.GetUserInfo(acc.token, acc.serverInfo.url);
    acc.userInfo = user1;
    return acc;
  }

  public static Base GenerateSimpleObject()
  {
    var @base = new Base
    {
      ["foo"] = "foo",
      ["bar"] = "bar",
      ["baz"] = "baz",
      ["now"] = DateTime.Now.ToString(CultureInfo.InvariantCulture)
    };

    return @base;
  }

  public static Base GenerateNestedObject()
  {
    var @base = new Base
    {
      ["foo"] = "foo",
      ["bar"] = "bar",
      ["@baz"] = new Base() { ["mux"] = "mux", ["qux"] = "qux" }
    };

    return @base;
  }

  public static Blob[] GenerateThreeBlobs()
  {
    return new[] { GenerateBlob("blob 1 data"), GenerateBlob("blob 2 data"), GenerateBlob("blob 3 data") };
  }

  private static Blob GenerateBlob(string content)
  {
    var filePath = Path.GetTempFileName();
    File.WriteAllText(filePath, content);
    return new Blob(filePath);
  }

  internal static async Task<Blob[]> SendBlobData(Account account, string projectId)
  {
    using ServerTransport remote = new(account, projectId);
    var blobs = Fixtures.GenerateThreeBlobs();
    Base myObject = new() { ["blobs"] = blobs };
    await Operations.Send(myObject, remote, false);
    return blobs;
  }
}

public class UserIdResponse
{
  public string userId { get; set; }
  public string apiToken { get; set; }
}
