using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Utils;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.DUI.Bridge;

/// <summary>
/// Result Pattern struct
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct Result<T>
{
  //Don't add new members to this struct, it is perfect.
  public T? Value { get; }
  public Exception? Exception { get; }

  [MemberNotNullWhen(false, nameof(Exception))]
  public bool IsSuccess => Exception is null;

  /// <summary>
  /// Create a successful result
  /// </summary>
  /// <param name="result"></param>
  public Result(T result)
  {
    Value = result;
  }

  /// <summary>
  /// Create a non-sucessful result
  /// </summary>
  /// <param name="result"></param>
  /// <exception cref="ArgumentNullException"><paramref name="result"/> was null</exception>
  public Result([NotNull] Exception? result)
  {
    Exception = result.NotNull();
  }
}

/// <summary>
/// The functions provided by this class are designed to be used in all "top level" scenarios (e.g. Plugin, UI, and Event callbacks)
/// To provide "last ditch effort" handling of unexpected exceptions that have not been handled.
///  1. Log events to the injected <see cref="ILogger"/>
///  2. Display a toast notification with exception details
/// <br/>
/// </summary>
/// <remarks>
/// <see cref="ExceptionHelpers.IsFatal"/> exceptions cannot be recovered from.
/// They will be rethrown to allow the host app to run its handlers<br/>
/// Depending on the host app, this may trigger windows event logging, and recovery snapshots before ultimately terminating the process<br/>
/// Attempting to swallow them may lead to data corruption, deadlocking, or things worse than a managed host app crash.
/// </remarks>
public sealed class TopLevelExceptionHandler
{
  private readonly ILogger<TopLevelExceptionHandler> _logger;
  private readonly IBridge? _bridge;
  private const string UNHANDLED_LOGGER_TEMPLATE = "An unhandled Exception occured";

  public TopLevelExceptionHandler(ILoggerFactory loggerFactory, IBridge? bridge = null)
  {
    _logger = loggerFactory.CreateLogger<TopLevelExceptionHandler>();
    _bridge = bridge;
  }

  /// <summary>
  /// Invokes the given function <paramref name="function"/> within a <see langword="try"/>/<see langword="catch"/> block,
  /// and provides exception handling for unexpected exceptions that have not been handled.<br/>
  /// </summary>
  /// <param name="function">The function to invoke and provide error handling for</param>
  /// <exception cref="Exception"><see cref="ExceptionHelpers.IsFatal"/> will be rethrown, these should be allowed to bubble up to the host app</exception>
  /// <seealso cref="ExceptionHelpers.IsFatal"/>
  public void CatchUnhandled(Action function)
  {
    CatchUnhandled(() =>
    {
      function.Invoke();
      return (object?)null;
    });
  }

  /// <inheritdoc cref="CatchUnhandled(Action)"/>
  /// <typeparam name="T"><paramref name="function"/> return type</typeparam>
  /// <returns>A result pattern struct (where exceptions have been handled)</returns>
  public Result<T> CatchUnhandled<T>(Func<T> function)
  {
    return CatchUnhandled(() => Task.FromResult(function.Invoke())).Result;
  }

  /// <inheritdoc cref="CatchUnhandled(Action)"/>
  public async Task CatchUnhandled(Func<Task> function)
  {
    await CatchUnhandled<object?>(async () =>
      {
        await function.Invoke().ConfigureAwait(false);
        return null;
      })
      .ConfigureAwait(false);
  }

  ///<inheritdoc cref="CatchUnhandled{T}(Func{T})"/>
  [SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "Top Level Exception Handler"
  )]
  public async Task<Result<T>> CatchUnhandled<T>(Func<Task<T>> function)
  {
    try
    {
      return new(await function.Invoke().ConfigureAwait(false));
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      _logger.LogError(ex, UNHANDLED_LOGGER_TEMPLATE);

      //TODO: On the UI side, we'll need to display exception messages using the UI Oguzhan made (with stack trace etc..)
      SetGlobalNotification(ToastNotificationType.DANGER, "Unhandled Exception Occured", ex.ToFormattedString(), false);
      return new(ex);
    }
    catch (Exception ex)
    {
      _logger.LogCritical(ex, UNHANDLED_LOGGER_TEMPLATE);
      throw;
    }
  }

  private void SetGlobalNotification(ToastNotificationType type, string title, string message, bool autoClose) =>
    _bridge?.Send(
      BasicConnectorBindingCommands.SET_GLOBAL_NOTIFICATION, //TODO: We could move these constants into a DUI3 constants static class
      new
      {
        type,
        title,
        description = message,
        autoClose
      }
    );
}
