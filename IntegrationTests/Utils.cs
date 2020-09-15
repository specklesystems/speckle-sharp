using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace TestsIntegration
{
  public static class Utils
  {
    public static Account SeedUser(ServerInfo server)
    {
      using (var client = new WebClient())
      {
        var seed_1 = Guid.NewGuid().ToString().ToLower();
        var user_1 = new NameValueCollection();
        user_1["email"] = $"{seed_1.Substring(0, 7)}@acme.com";
        user_1["password"] = "12ABC3456789DEF0GHO";
        user_1["name"] = $"{seed_1.Substring(0, 5)} Name";
        user_1["username"] = $"{seed_1.Substring(0, 5)}_Name";

        var raw_1 = client.UploadValues("http://127.0.0.1:3000/auth/local/register", "POST", user_1);
        var info_1 = JsonConvert.DeserializeObject<UserIdResponse>(Encoding.UTF8.GetString(raw_1));

        return new Account { token = "Bearer " + info_1.apiToken, userInfo = new UserInfo { id = info_1.userId, email = user_1["email"] }, serverInfo = server };
      }
    }
  }

  public class UserIdResponse
  {
    public string userId { get; set; }
    public string apiToken { get; set; }
  }
}
