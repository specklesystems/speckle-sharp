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
    int timeoutSeconds = DEFAULT_TIMEOUT_SECONDS
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

    var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(timeoutSeconds);

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
  /// Sends a <c>GET</c> request to the provided <paramref name="uri"/>
  /// </summary>
  /// <param name="uri">The URI that should be pinged</param>
  /// <exception cref="HttpRequestException">Request to <paramref name="uri"/> failed</exception>
  public static async Task<HttpResponseMessage> HttpPing(Uri uri)
  {
    try
    {
      using var httpClient = GetHttpProxyClient();
      HttpResponseMessage response = await httpClient.GetAsync(uri).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();
      SpeckleLog.Logger.Information("Successfully pinged {uri}", uri);
      return response;
    }
    catch (HttpRequestException ex)
    {
      SpeckleLog.Logger.Warning(ex, "Ping to {uri} was unsuccessful: {message}", uri, ex.Message);
      throw new HttpRequestException($"Ping to {uri} was unsuccessful", ex);
    }
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
      SpeckleLog.Logger
        .ForContext("ExceptionType", policyResult.FinalException?.GetType())
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
