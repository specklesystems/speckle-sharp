using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Speckle.Connectors.Utils;

public static class NotNullExtensions
{
  /// <inheritdoc cref="NotNull{T}(T?,string?)"/>
  public static async Task<T> NotNull<T>(
    this Task<T?> task,
    [CallerArgumentExpression(nameof(task))] string? message = null
  )
    where T : class
  {
    var x = await task.ConfigureAwait(false);
    if (x is null)
    {
      throw new ArgumentNullException(message ?? "Value is null");
    }
    return x;
  }

  /// <inheritdoc cref="NotNull{T}(T?,string?)"/>
  public static async Task<T> NotNull<T>(
    this Task<T?> task,
    [CallerArgumentExpression(nameof(task))] string? message = null
  )
    where T : struct
  {
    var x = await task.ConfigureAwait(false);
    if (x is null)
    {
      throw new ArgumentNullException(message ?? "Value is null");
    }
    return x.Value;
  }

  /// <param name="obj">the object to check for null</param>
  /// <param name="paramName">see <see cref="CallerArgumentExpressionAttribute"/></param>
  /// <typeparam name="T"><paramref name="obj"/> type</typeparam>
  /// <returns>A non null <typeparamref name="T"/> value</returns>
  /// <exception cref="ArgumentNullException"><paramref name="obj"/> was null</exception>
  public static T NotNull<T>([NotNull] this T? obj, [CallerArgumentExpression(nameof(obj))] string? paramName = null)
    where T : class
  {
    if (obj is null)
    {
      throw new ArgumentNullException(paramName ?? "Value is null");
    }
    return obj;
  }

  /// <inheritdoc cref="NotNull{T}(T?,string?)"/>
  public static T NotNull<T>([NotNull] this T? obj, [CallerArgumentExpression(nameof(obj))] string? paramName = null)
    where T : struct
  {
    if (obj is null)
    {
      throw new ArgumentNullException(paramName ?? "Value is null");
    }
    return obj.Value;
  }
}
