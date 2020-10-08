using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Transports;

namespace TestsIntegration
{
  public static class Fixtures
  {
    private static SQLiteTransport AccountStorage = new SQLiteTransport(scope: "Accounts");

    public static void UpdateOrSaveAccount(Account account)
    {
      AccountStorage.DeleteObject(account.id);
      AccountStorage.SaveObjectSync(account.id, JsonConvert.SerializeObject(account));
    }

    public static void DeleteLocalAccount(string id)
    {
      AccountStorage.DeleteObject(id);
    }

    public static Account SeedUser(ServerInfo server)
    {
      using (var client = new WebClient())
      {
        var seed = Guid.NewGuid().ToString().ToLower();
        var user = new NameValueCollection();
        user["email"] = $"{seed.Substring(0, 7)}@acme.com";
        user["password"] = "12ABC3456789DEF0GHO";
        user["name"] = $"{seed.Substring(0, 5)} Name";

        var raw = client.UploadValues("http://127.0.0.1:3000/auth/local/register", "POST", user);
        var info = JsonConvert.DeserializeObject<UserIdResponse>(Encoding.UTF8.GetString(raw));

        return new Account { token = "Bearer " + info.apiToken, userInfo = new UserInfo { id = info.userId, email = user["email"] }, serverInfo = server };
      }
    }
  }

  public class UserIdResponse
  {
    public string userId { get; set; }
    public string apiToken { get; set; }
  }
}
