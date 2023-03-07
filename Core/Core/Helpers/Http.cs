# nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;
using Speckle.Core.Credentials;
using Polly.Extensions.Http;
using Polly.Retry;
using System.Diagnostics;
using Serilog.Context;
using System.Net.Sockets;

namespace Speckle.Core.Helpers
{
  public static class Http
  {
    public static IEnumerable<TimeSpan> DefaultDelay() =>
      Backoff.DecorrelatedJitterBackoffV2(
        medianFirstRetryDelay: TimeSpan.FromMilliseconds(100),
        retryCount: 5
      );

    /// <summary>
    /// Policy for retrying failing Http requests
    /// </summary>
    [Obsolete(
      "All http requests are now retried by the client provided in the GetHttpProxyClient method, there is no need to add retries on top",
      true
    )]
    public static Policy<bool> HttpRetryPolicy = Policy
      .Handle<Exception>()
      .OrResult<bool>(r => r.Equals(false))
      .WaitAndRetry(
        DefaultDelay(),
        (exception, timeSpan, retryAttempt, context) =>
        {
          Log.Information("Retrying #{retryAttempt}...", retryAttempt);
        }
      );

    /// <summary>
    /// Policy for retrying failing Http requests
    /// </summary>
    [Obsolete(
      "All http requests are now retried by the client provided in the GetHttpProxyClient method, there is no need to add retries on top",
      true
    )]
    public static AsyncPolicy<bool> HttpRetryAsyncPolicy = Policy
      .Handle<Exception>()
      .OrResult<bool>(r => r.Equals(false))
      .WaitAndRetryAsync(
        DefaultDelay(),
        (exception, timeSpan, retryAttempt, context) =>
        {
          Log.Information("Retrying #{retryAttempt}...", retryAttempt);
        }
      );

    public static AsyncRetryPolicy<HttpResponseMessage> HttpAsyncPolicy(
      IEnumerable<TimeSpan>? delay = null
    ) =>
      HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
          delay ?? DefaultDelay(),
          onRetry: (ex, timeSpan, retryAttempt, context) =>
          {
            context.Remove("retryCount");
            context.Add("retryCount", retryAttempt);
            Log.Information(
              ex.Exception,
              "The http request failed with {exceptionType} exception retrying after {cooldown} miliseconds. This is retry attempt {retryAttempt}",
              ex.GetType().Name,
              timeSpan.TotalSeconds * 1000,
              retryAttempt
            );
          }
        );

    /// <summary>
    /// Checks if the user has a valid internet connection by first pinging cloudfare (fast)
    /// and then trying get from the default Speckle server (slower)
    /// Each check is retried 3 times
    /// </summary>
    /// <returns>True if the user is connected to the internet, false otherwise.</returns>
    public static async Task<bool> UserHasInternet()
    {
      //can ping cloudfare, skip further checks
      //this method should be the fastest
      if (await Ping("1.1.1.1"))
        return true;

      //lastly, try getting the default Speckle server, in case this is a sandboxed environment
      string defaultServer = AccountManager.GetDefaultServerUrl();
      bool hasInternet = await HttpPing(defaultServer);

      if (!hasInternet)
        Log.ForContext("defaultServer", defaultServer).Warning("Failed to ping internet");

      return hasInternet;
    }

    /// <summary>
    /// Pings a specific url to verify it's accessible. Retries 3 times.
    /// </summary>
    /// <param name="hostnameOrAddress">The hostname or address to ping.</param>
    /// <returns>True if the the status code is 200, false otherwise.</returns>
    public static async Task<bool> Ping(string hostnameOrAddress)
    {
      Log.Information("Pinging {hostnameOrAddress}", hostnameOrAddress);
      var policy = Policy
        .Handle<PingException>()
        .Or<SocketException>()
        .WaitAndRetryAsync(
          DefaultDelay(),
          (ex, timeSpan, retryAttempt, context) =>
          {
            Log.Information(
              ex,
              "The http request failed with {exceptionType} exception retrying after {cooldown} miliseconds. This is retry attempt {retryAttempt}",
              ex.GetType().Name,
              timeSpan.TotalSeconds * 1000,
              retryAttempt
            );
          }
        );
      var policyResult = await policy.ExecuteAndCaptureAsync(async () =>
      {
        Ping myPing = new Ping();
        var hostname =
          (Uri.CheckHostName(hostnameOrAddress) != UriHostNameType.Unknown)
            ? hostnameOrAddress
            : (new Uri(hostnameOrAddress)).DnsSafeHost;
        byte[] buffer = new byte[32];
        int timeout = 1000;
        PingOptions pingOptions = new PingOptions();
        PingReply reply = await myPing.SendPingAsync(hostname, timeout, buffer, pingOptions);
        return (reply.Status == IPStatus.Success);
      });
      if (policyResult.Result == true)
        return true;
      Log.Warning(
        policyResult.FinalException,
        "Failed to ping {hostnameOrAddress} cause: {exceptionMessage}",
        policyResult.FinalException.Message
      );
      return false;
    }

    /// <summary>
    /// Pings and tries gettign data from a specific address to verify it's online. Retries 3 times.
    /// </summary>
    /// <param name="address">The address to use</param>
    /// <returns>True if the the status code is successful, false otherwise.</returns>
    public static async Task<bool> HttpPing(string address)
    {
      Log.Information("HttpPinging {address}", address);
      try
      {
        var _httpClient = GetHttpProxyClient();
        var response = await _httpClient.GetAsync(address);
        return response.IsSuccessStatusCode;
      }
      catch (Exception ex)
      {
        Log.Warning(ex, "Exception while pinging: {message}", ex.Message);
        return false;
      }
    }

    public static HttpClient GetHttpProxyClient(SpeckleHttpClientHandler? handler = null)
    {
      IWebProxy proxy = WebRequest.GetSystemWebProxy();
      proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;

      return new HttpClient(handler ?? new SpeckleHttpClientHandler());
    }
  }

  public class SpeckleHttpClientHandler : HttpClientHandler
  {
    private IEnumerable<TimeSpan> _delay;

    public SpeckleHttpClientHandler(IEnumerable<TimeSpan>? delay = null) : base()
    {
      _delay = delay ?? Http.DefaultDelay();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
      HttpRequestMessage request,
      CancellationToken cancellationToken
    )
    {
      // this is a preliminary client server correlation implementation
      // refactor this, when we have a better observability stack
      var context = new Context();
      using (LogContext.PushProperty("correlationId", context.CorrelationId))
      using (LogContext.PushProperty("targetUrl", request.RequestUri))
      using (LogContext.PushProperty("httpMethod", request.Method))
      {
        Log.Debug("Starting execution of http request to {targetUrl}", request.RequestUri);
        var timer = new Stopwatch();
        timer.Start();
        context.Add("retryCount", 0);
        var policyResult = await Http.HttpAsyncPolicy(_delay)
          .ExecuteAndCaptureAsync(
            ctx =>
            {
              request.Headers.Add("x-request-id", ctx.CorrelationId.ToString());
              return base.SendAsync(request, cancellationToken);
            },
            context
          );
        timer.Stop();
        var status = policyResult.Outcome == OutcomeType.Successful ? "succeeded" : "failed";
        context.TryGetValue("retryCount", out var retryCount);
        Log.Information(
          "Execution of http request to {targetUrl} {resultStatus} with {httpStatusCode} after {elapsed} seconds and {retryCount} retries",
          request.RequestUri,
          status,
          policyResult.Result.StatusCode,
          timer.Elapsed.TotalSeconds,
          retryCount ?? 0
        );
        if (policyResult.Outcome == OutcomeType.Successful)
          return policyResult.Result;

        var statusCode = policyResult.Result.StatusCode;
        // should we wrap this exception into something Speckle specific?
        throw policyResult.FinalException;
      }
    }
  }
}
