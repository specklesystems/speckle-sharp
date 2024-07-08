﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Speckle.Converters.Common;

public static class NotNullExtensions
{
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

  public static T NotNull<T>([NotNull] this T? obj, [CallerArgumentExpression(nameof(obj))] string? paramName = null)
    where T : class
  {
    if (obj is null)
    {
      throw new ArgumentNullException(paramName ?? "Value is null");
    }
    return obj;
  }

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
