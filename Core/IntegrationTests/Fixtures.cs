using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Speckle.Core.Api;
using Speckle.Core.Credentials;

namespace TestsIntegration
{
  public static class Fixtures
  {
    public static readonly ServerInfo Server = new ServerInfo { url = "http://localhost:3000", name = "Docker Server" };

    public static Account SeedUser()
    {
      var seed = Guid.NewGuid().ToString().ToLower();
      var user = new Dictionary<string, string>();
      user["email"] = $"{seed.Substring(0, 7)}@acme.com";
      user["password"] = "12ABC3456789DEF0GHO";
      user["name"] = $"{seed.Substring(0, 5)} Name";

      var registerRequest = (HttpWebRequest)WebRequest.Create($"{Server.url}/auth/local/register?challenge=challengingchallenge");
      registerRequest.Method = "POST";
      registerRequest.ContentType = "application/json";
      registerRequest.AllowAutoRedirect = false;


      using (var streamWriter = new StreamWriter(registerRequest.GetRequestStream()))
      {
        string json = JsonConvert.SerializeObject(user);
        streamWriter.Write(json);
        streamWriter.Flush();
      }

      WebResponse response;
      string redirectUrl = null;
      try
      {
        response = registerRequest.GetResponse();
        redirectUrl = response.Headers[HttpResponseHeader.Location];
        Debug.WriteLine(redirectUrl);
      }
      catch (WebException e)
      {
        if (e.Message.Contains("302"))
        {
          Console.WriteLine("We are redirected!");
          response = e.Response;
          redirectUrl = e.Response.Headers[HttpResponseHeader.Location];
          Console.WriteLine("We are redirected; but in an error.");
          Console.WriteLine(redirectUrl);
        }
      }

      var tokenRequest = (HttpWebRequest)WebRequest.Create($"{Server.url}/auth/token");
      tokenRequest.Method = "POST";
      tokenRequest.ContentType = "application/json";

      Console.WriteLine(redirectUrl);
      Console.WriteLine("Why do the tests pass locally?");
      var accessCode = redirectUrl.Split("?access_code=")[1];
      var tokenBody = new Dictionary<string, string>()
      {
        ["accessCode"] = accessCode,
        ["appId"] = "spklwebapp",
        ["appSecret"] = "spklwebapp",
        ["challenge"] = "challengingchallenge"
      };

      using (var streamWriter = new StreamWriter(tokenRequest.GetRequestStream()))
      {
        string json = JsonConvert.SerializeObject(tokenBody);
        streamWriter.Write(json);
        streamWriter.Flush();
      }

      var tokenResponse = tokenRequest.GetResponse();
      var deserialised = new Dictionary<string, string>();
      using (var streamReader = new StreamReader(tokenResponse.GetResponseStream()))
      {
        var text = streamReader.ReadToEnd();
        deserialised = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
      }

      var acc = new Account { token = deserialised["token"], userInfo = new UserInfo { id = user["name"], email = user["email"] }, serverInfo = Server };
      var client = new Client(acc);

      var user1 = client.UserGet().Result;
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
