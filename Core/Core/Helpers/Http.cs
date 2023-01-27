using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;
using Speckle.Core.Credentials;

namespace Speckle.Core.Helpers
{
  public static class Http
  {

    private static IEnumerable<TimeSpan> delay = Backoff.DecorrelatedJitterBackoffV2(
       medianFirstRetryDelay: TimeSpan.FromMilliseconds(100),
       retryCount: 3
     );
    /// <summary>
    /// Policy for retrying failing Http requests
    /// </summary>
    public static Policy<bool> HttpRetryPolicy = Policy
        .Handle<Exception>()
        .OrResult<bool>(r => r.Equals(false))
        .WaitAndRetry(
          delay,
          (exception, timeSpan, retryAttempt, context) =>
          {
            Log.Information("Retrying #{retryAttempt}...", retryAttempt);
          });

    /// <summary>
    /// Policy for retrying failing Http requests
    /// </summary>
    public static AsyncPolicy<bool> HttpRetryAsyncPolicy = Policy
        .Handle<Exception>()
        .OrResult<bool>(r => r.Equals(false))
        .WaitAndRetryAsync(
          delay,
          (exception, timeSpan, retryAttempt, context) =>
          {
            Log.Information("Retrying #{retryAttempt}...", retryAttempt);
          });

    /// <summary>
    /// Checks if the user has a valid internet connection by first pinging cloudfare (fast)
    /// and then trying get from the default Speckle server (slower)
    /// Each check is retried 2 times every 200ms
    /// </summary>
    /// <returns>True if the user is connected to the internet, false otherwise.</returns>
    public static async Task<bool> UserHasInternet()
    {

      //can ping cloudfare, skip further checks
      //this method should be the fastest
      if (await Ping("1.1.1.1"))
        return true;


      //lastly, try getting the default Speckle server, in case this is a sandboxed environment
      return await HttpPing(AccountManager.GetDefaultServerUrl());
    }


    /// <summary>
    /// Pings a specific url to verify it's accessible. Retries 2 times.
    /// </summary>
    /// <param name="hostnameOrAddress">The hostname or address to ping.</param>
    /// <returns>True if the the status code is 200, false otherwise.</returns>
    public static async Task<bool> Ping(string hostnameOrAddress)
    {
      Log.Information("Pinging {hostnameOrAddress}", hostnameOrAddress);
      return HttpRetryPolicy.Execute(() =>
      {
        try
        {
          Ping myPing = new Ping();
          var hostname = (Uri.CheckHostName(hostnameOrAddress) != UriHostNameType.Unknown) ? hostnameOrAddress : (new Uri(hostnameOrAddress)).DnsSafeHost;
          byte[] buffer = new byte[32];
          int timeout = 1000;
          PingOptions pingOptions = new PingOptions();
          PingReply reply = myPing.Send(hostname, timeout, buffer, pingOptions);
          return (reply.Status == IPStatus.Success);
        }
        catch (Exception ex)
        {
          Log.Information("Exception while pinging: {message}", ex.Message);
          return false;
        }
      });
    }

    /// <summary>
    /// Pings and tries gettign data from a specific address to verify it's online. Retries 2 times.
    /// </summary>
    /// <param name="address">The address to use</param>
    /// <returns>True if the the status code is successful, false otherwise.</returns>
    public static async Task<bool> HttpPing(string address)
    {
      Log.Information("HttpPinging {address}", address);
      return await HttpRetryAsyncPolicy.ExecuteAsync(async () =>
      {
        try
        {
          HttpClient _httpClient = GetHttpProxyClient();

          _httpClient.Timeout = TimeSpan.FromSeconds(1);
          var response = await _httpClient.GetAsync(address);
          return response.IsSuccessStatusCode;

        }
        catch (Exception ex)
        {
          Log.Information("Exception while pinging: {message}", ex.Message);
          return false;
        }
      });
    }

    public static HttpClient GetHttpProxyClient(HttpClientHandler handler = null)
    {

      IWebProxy proxy = WebRequest.GetSystemWebProxy();
      proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

      if (handler == null)
      {
        handler = new HttpClientHandler();
      }
      handler.Proxy = proxy;
      handler.PreAuthenticate = true;

      var client = new HttpClient(handler);

      return client;
    }
  }
}
