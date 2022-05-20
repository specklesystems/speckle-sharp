using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Speckle.Core.Credentials;
using Speckle.Newtonsoft.Json;

namespace Speckle.Core.Logging
{
  /// <summary>
  ///  Anonymous telemetry to help us understand how to make a better Speckle.
  ///  This really helps us to deliver a better open source project and product!
  /// </summary>
  public static class Analytics
  {
    private const string MixpanelToken = "acd87c5a50b56df91a795e999812a3a4";
    private const string MixpanelServer = "https://analytics.speckle.systems";

    /// <summary>
    /// Default Mixpanel events
    /// </summary>
    public enum Events
    {
      /// <summary>
      /// Event triggered when data is sent to a Speckle Server
      /// </summary>
      Send,
      /// <summary>
      /// Event triggered when data is received from a Speckle Server
      /// </summary>
      Receive,
      /// <summary>
      /// Event triggered when a node is executed in a visual programming environment, it should contain the name of the action and the host application
      /// </summary>
      NodeRun,
      /// <summary>
      /// Event triggered when an action is executed in Desktop UI, it should contain the name of the action and the host application
      /// </summary>
      DUIAction,
      /// <summary>
      /// Event triggered when a node is first created in a visual programming environment, it should contain the name of the action and the host application
      /// </summary>
      NodeCreate,
    };


    /// <summary>
    /// Cached email
    /// </summary>
    private static string LastEmail { get; set; }
    /// <summary>
    /// Cached server URL
    /// </summary>
    private static string LastServer { get; set; }

    /// <summary>
    /// Tracks an event without specifying the email and server.
    /// It's not always possible to know which account the user has selected, especially in visual programming.
    /// Therefore we are caching the email and server values so that they can be used also when nodes such as "Serialize" are used.
    /// If no account info is cached, we use the default account data.
    /// </summary>
    /// <param name="eventName">Name of the even</param>
    /// <param name="customProperties">Additional parameters to pass in to event</param>
    public static void TrackEvent(Events eventName, Dictionary<string, object> customProperties = null)
    {
      string email = "";
      string server = "";

      if (LastEmail != null && LastServer != null)
      {
        email = LastEmail;
        server = LastServer;
      }
      else
      {
        var acc = Credentials.AccountManager.GetDefaultAccount();
        if (acc == null)
          return;

        email = acc.userInfo.email;
        server = acc.serverInfo.url;
      }

      TrackEvent(email, server, eventName, customProperties);
    }

    /// <summary>
    /// Tracks an event from a specified account, anonymizes personal information
    /// </summary>
    /// <param name="account">Account to use, it will be anonymized</param>
    /// <param name="eventName">Name of the event</param>
    /// <param name="customProperties">Additional parameters to pass to the event</param>
    public static void TrackEvent(Account account, Events eventName, Dictionary<string, object> customProperties = null)
    {
      if (account == null)
        TrackEvent("unknown", "https://speckle.xyz/", eventName, customProperties);
      else
        TrackEvent(account.userInfo.email, account.serverInfo.url, eventName, customProperties);
    }

    /// <summary>
    /// Tracks an event from a specified email and server, anonymizes personal information
    /// </summary>
    /// <param name="email">Email of the user, it will be anonymized</param>
    /// <param name="server">Server URL, it will be anonymized</param>
    /// <param name="eventName">Name of the event</param>
    /// <param name="customProperties">Additional parameters to pass to the event</param>
    private static void TrackEvent(string email, string server, Events eventName, Dictionary<string, object> customProperties = null)
    {
      LastEmail = email;
      LastServer = server;

#if DEBUG
      //only track in prod
      //return;
#endif

      Task.Run(() =>
      {

        try
        {
          var httpWebRequest = (HttpWebRequest)WebRequest.Create(MixpanelServer + "/track?ip=1");
          httpWebRequest.ContentType = "application/x-www-form-urlencoded";
          httpWebRequest.Accept = "text/plain";
          httpWebRequest.Method = "POST";
          ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

          server = CleanURL(server);

          var hashedEmail = "@" + Hash(email); //prepending an @ so we distinguish logged and non-logged users
          var hashedServer = Hash(server);

          var properties = new Dictionary<string, object>()
          {
            { "distinct_id", hashedEmail },
            { "server_id", hashedServer },
            { "token", MixpanelToken },
            { "hostApp", Setup.HostApplication },
            { "hostAppVersion", Setup.VersionedHostApplication },
            { "core_version", Assembly.GetExecutingAssembly().GetName().Version.ToString()},
            { "$os",  GetOs() },
            { "type", "action" }
          };

          if (customProperties != null)
            properties = properties.Concat(customProperties).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);


          using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
          {
            string json = JsonConvert.SerializeObject(new
            {

              @event = eventName.ToString(),
              properties
            });

            streamWriter.Write("data=" + HttpUtility.UrlEncode(json));
            streamWriter.Flush();
            streamWriter.Close();
          }

          var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
          using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
          {
            var result = streamReader.ReadToEnd();
          }
        }
        catch (Exception e)
        {
          // POKEMON: Gotta catch 'em all!
        }

      });

    }

    private static string CleanURL(string server)
    {
      Uri NewUri;

      if (Uri.TryCreate(server, UriKind.Absolute, out NewUri))
      {
        server = NewUri.Authority;
      }
      return server;
    }

    private static string Hash(string input)
    {

      using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
      {
        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input.ToLowerInvariant());
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
          sb.Append(hashBytes[i].ToString("X2"));
        }
        return sb.ToString();
      }

    }

    private static string GetOs()
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "Mac OS X";
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
      return "Unknown";
    }

  }
}
