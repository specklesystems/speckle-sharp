using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.Utils;
using Speckle.Core.Logging;
using Speckle.Core.Models.Extensions;

namespace Speckle.Connectors.DUI.Bridge;

//Don't add to this struct, it is perfect.
public readonly struct Result<T>
{
  public T? Value { get; }
  public Exception? Exception { get; }

  [MemberNotNullWhen(false, nameof(Exception))]
  public bool IsSuccess => Exception is null;

  public Result(T result)
  {
    Value = result;
  }

  public Result([NotNull] Exception? exception)
  {
    Exception = exception.NotNull();
  }
}

public class TopLevelExceptionHandler
{
  private readonly ILogger _logger;
  private readonly IBridge? _bridge;
  private const string UNHANDLED_LOGGER_TEMPLATE = "An unhandled Exception occured";

  public TopLevelExceptionHandler(ILogger<TopLevelExceptionHandler> logger, IBridge? bridge = null)
  {
    _logger = logger;
    _bridge = bridge;
  }

  public void CatchUnhandled(Action action)
  {
    CatchUnhandled(() =>
    {
      action.Invoke();
      return (object?)null;
    });
  }

  public Result<T> CatchUnhandled<T>(Func<T> action)
  {
    return CatchUnhandled(() => Task.FromResult(action.Invoke())).Result;
  }

  /// <summary>
  /// Invokes <paramref name="function"/> within a <see langword="try"/> block, providing exception handling for unexpected exceptions
  ///
  /// </summary>
  /// <param name="function">The <see cref="Func{T}"/> to invoke</param>
  /// <typeparam name="T"></typeparam>
  /// <returns>Result pattern for the resulting action</returns>
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
      //TODO: Figure out of this is the best way to display an exception, chat with Oguzhan, perhaps we need some changes on the UI side
      SetGlobalNotification(ToastNotificationType.DANGER, "Unhandled Exception Occured", ex.ToFormattedString(), false);
      return new(ex);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, UNHANDLED_LOGGER_TEMPLATE);
      throw;
    }
  }

  private void SetGlobalNotification(ToastNotificationType type, string title, string message, bool autoClose = true) =>
    _bridge?.Send(
      BasicConnectorBindingCommands.SET_GLOBAL_NOTIFICATION, //TODO: Hack to reference this const
      new
      {
        type,
        title,
        description = message,
        autoClose
      }
    );
}
