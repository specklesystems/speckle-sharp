using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Timeout;
using Serilog.Context;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.Core.Helpers;

public static class Http
{
  public const int DEFAULT_TIMEOUT_SECONDS = 60;

  public static IEnumerable<TimeSpan> DefaultDelay()
  {
    return Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(200), 5);
  }

  public static IAsyncPolicy<HttpResponseMessage> HttpAsyncPolicy(
    IEnumerable<TimeSpan>? delay = null,
    int timeoutSeconds = DEFAULT_TIMEOUT_SECONDS,
    TimeoutStrategy timeoutStrategy = TimeoutStrategy.Optimistic
  )
  {
    var retryPolicy = HttpPolicyExtensions
      .HandleTransientHttpError()
      .Or<TimeoutRejectedException>()
      .WaitAndRetryAsync(
        delay ?? DefaultDelay(),
        (ex, timeSpan, retryAttempt, context) =>
        {
          context.Remove("retryCount");
          context.Add("retryCount", retryAttempt);
        }
      );

    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds, timeoutStrategy);

    return Policy.WrapAsync(retryPolicy, timeoutPolicy);
  }

  /// <summary>
  /// Checks if the user has a valid internet connection by first pinging cloudfare (fast)
  /// and then trying get from the default Speckle server (slower)
  /// </summary>
  /// <returns>True if the user is connected to the internet, false otherwise.</returns>
  public static async Task<bool> UserHasInternet()
  {
    string? defaultServer = null;
    try
    {
      //Perform a quick ping test e.g. to cloudflaire dns, as is quicker than pinging server
      if (await Ping("1.1.1.1").ConfigureAwait(false))
      {
        return true;
      }

      defaultServer = AccountManager.GetDefaultServerUrl();
      Uri serverUrl = new(defaultServer);
      await HttpPing(serverUrl).ConfigureAwait(false);
      return true;
    }
    catch (HttpRequestException ex)
    {
      SpeckleLog.Logger.ForContext("defaultServer", defaultServer).Warning(ex, "Failed to ping internet");

      return false;
    }
  }

  /// <summary>
  /// Pings a specific url to verify it's accessible. Retries 3 times.
  /// </summary>
  /// <param name="hostnameOrAddress">The hostname or address to ping.</param>
  /// <returns>True if the the status code is 200, false otherwise.</returns>
  public static async Task<bool> Ping(string hostnameOrAddress)
  {
    SpeckleLog.Logger.Information("Pinging {hostnameOrAddress}", hostnameOrAddress);
    var policy = Policy
      .Handle<PingException>()
      .Or<SocketException>()
      .WaitAndRetryAsync(
        DefaultDelay(),
        (ex, timeSpan, retryAttempt, context) => {
          //Log.Information(
          //  ex,
          //  "The http request failed with {exceptionType} exception retrying after {cooldown} milliseconds. This is retry attempt {retryAttempt}",
          //  ex.GetType().Name,
          //  timeSpan.TotalSeconds * 1000,
          //  retryAttempt
          //);
        }
      );
    var policyResult = await policy
      .ExecuteAndCaptureAsync(async () =>
      {
        Ping myPing = new();
        var hostname =
          Uri.CheckHostName(hostnameOrAddress) != UriHostNameType.Unknown
            ? hostnameOrAddress
            : new Uri(hostnameOrAddress).DnsSafeHost;
        byte[] buffer = new byte[32];
        int timeout = 1000;
        PingOptions pingOptions = new();
        PingReply reply = await myPing.SendPingAsync(hostname, timeout, buffer, pingOptions).ConfigureAwait(false);
        if (reply.Status != IPStatus.Success)
        {
          throw new SpeckleException($"The ping operation failed with status {reply.Status}");
        }

        return true;
      })
      .ConfigureAwait(false);
    if (policyResult.Outcome == OutcomeType.Successful)
    {
      return true;
    }

    SpeckleLog.Logger.Warning(
      policyResult.FinalException,
      "Failed to ping {hostnameOrAddress} cause: {exceptionMessage}",
      policyResult.FinalException.Message
    );
    return false;
  }

  /// <summary>
  /// Sends a <c>GET</c> request to the provided <paramref name="speckleServerUrl"/>
  /// </summary>
  /// <param name="speckleServerUrl">The URI that should be pinged</param>
  /// <exception cref="HttpRequestException">Request to <paramref name="speckleServerUrl"/> failed</exception>
  public static async Task<HttpResponseMessage> HttpPing(
    Uri speckleServerUrl,
    CancellationToken cancellationToken = default
  )
  {
    using var httpClient = GetHttpProxyClient(
      new SpeckleHttpClientHandler(HttpAsyncPolicy(timeoutSeconds: 15, timeoutStrategy: TimeoutStrategy.Pessimistic))
    );

    //GETing the root uri has auth related overheads, so we'd prefer to ping a static resource.
    //This is setup to be super compatible with older servers that don't have a /api/ping endpoint, and self hosting which may not have a favicon
    Uri[] pingUrls = { GetPingUrl(speckleServerUrl), GetFaviconUrl(speckleServerUrl), speckleServerUrl };
    List<Exception> failures = new();
    foreach (var ping in pingUrls)
    {
      var response = await httpClient.GetAsync(ping, cancellationToken).ConfigureAwait(false);
      try
      {
        response.EnsureSuccessStatusCode();
        SpeckleLog.Logger.Debug("Successfully pinged {uri}", speckleServerUrl);
        return response;
      }
      catch (HttpRequestException ex)
      {
        failures.Add(ex);
      }
    }

    AggregateException ax = new(failures);
    SpeckleLog.Logger.Warning(ax, $"Ping to {speckleServerUrl} was unsuccessful", speckleServerUrl);
    throw new HttpRequestException($"Ping to {speckleServerUrl} was unsuccessful", ax);
  }

  public static Uri GetPingUrl(Uri serverUrl)
  {
    var server = serverUrl.GetLeftPart(UriPartial.Authority);
    return new Uri(new(server), "/api/ping");
  }

  public static Uri GetFaviconUrl(Uri serverUrl)
  {
    var server = serverUrl.GetLeftPart(UriPartial.Authority);
    return new Uri(new(server), "/favicon.ico");
  }

  public static HttpClient GetHttpProxyClient(SpeckleHttpClientHandler? speckleHttpClientHandler = null)
  {
    IWebProxy proxy = WebRequest.GetSystemWebProxy();
    proxy.Credentials = CredentialCache.DefaultCredentials;

    speckleHttpClientHandler ??= new SpeckleHttpClientHandler(HttpAsyncPolicy());

    var client = new HttpClient(speckleHttpClientHandler)
    {
      Timeout = Timeout.InfiniteTimeSpan //timeout is configured on the SpeckleHttpClientHandler through policy
    };
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.UserAgent.Add(new("SpeckleSDK", "2.0.0"));
    return client;
  }

  public static bool CanAddAuth(string? authToken, out string? bearerHeader)
  {
    if (!string.IsNullOrEmpty(authToken))
    {
      bearerHeader = authToken!.ToLowerInvariant().Contains("bearer") ? authToken : $"Bearer {authToken}";
      return true;
    }

    bearerHeader = null;
    return false;
  }

  public static void AddAuthHeader(HttpClient client, string? authToken)
  {
    if (CanAddAuth(authToken, out string? value))
    {
      client.DefaultRequestHeaders.Add("Authorization", value);
    }
  }
}

public sealed class SpeckleHttpClientHandler : HttpClientHandler
{
  private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

  public SpeckleHttpClientHandler(IAsyncPolicy<HttpResponseMessage> resiliencePolicy)
  {
    _resiliencePolicy = resiliencePolicy;
  }

  /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> requested cancel</exception>
  /// <exception cref="HttpRequestException">Send request failed</exception>
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
      SpeckleLog.Logger.Debug("Starting execution of http request to {targetUrl}", request.RequestUri);
      var timer = new Stopwatch();
      timer.Start();
      context.Add("retryCount", 0);

      request.Headers.Add("x-request-id", context.CorrelationId.ToString());

      var policyResult = await _resiliencePolicy
        .ExecuteAndCaptureAsync(
          ctx =>
          {
            return base.SendAsync(request, cancellationToken);
          },
          context
        )
        .ConfigureAwait(false);
      timer.Stop();
      var status = policyResult.Outcome == OutcomeType.Successful ? "succeeded" : "failed";
      context.TryGetValue("retryCount", out var retryCount);
      SpeckleLog
        .Logger.ForContext("ExceptionType", policyResult.FinalException?.GetType())
        .Information(
          "Execution of http request to {httpScheme}://{hostUrl}{relativeUrl} {resultStatus} with {httpStatusCode} after {elapsed} seconds and {retryCount} retries. Request correlation ID: {correlationId}",
          request.RequestUri.Scheme,
          request.RequestUri.Host,
          request.RequestUri.PathAndQuery,
          status,
          policyResult.Result?.StatusCode,
          timer.Elapsed.TotalSeconds,
          retryCount ?? 0,
          context.CorrelationId.ToString()
        );
      if (policyResult.Outcome == OutcomeType.Successful)
      {
        return policyResult.Result!;
      }

      // if the policy failed due to a cancellation, AND it was our cancellation token, then don't wrap the exception, and rethrow an new cancellation
      if (policyResult.FinalException is OperationCanceledException)
      {
        cancellationToken.ThrowIfCancellationRequested();
      }

      throw new HttpRequestException("Policy Failed", policyResult.FinalException);
    }
  }
}
