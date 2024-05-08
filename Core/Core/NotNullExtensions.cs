using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.Core;

public static class NotNullExtensions
{
  public static IEnumerable<T> Empty<T>(this IEnumerable<T>? source) => source ?? Enumerable.Empty<T>();

  public static async Task<T> NotNull<T>(this Task<T?> task, string? message = null)
    where T : class
  {
    var x = await task.ConfigureAwait(false);
    if (x is null)
    {
      throw new ArgumentNullException(message ?? "Value is null");
    }
    return x;
  }

  public static async Task<T> NotNull<T>(this Task<T?> task, string? message = null)
    where T : struct
  {
    var x = await task.ConfigureAwait(false);
    if (x is null)
    {
      throw new ArgumentNullException(message ?? "Value is null");
    }
    return x.Value;
  }

  public static T NotNull<T>(this T? obj, string? message = null)
    where T : class
  {
    if (obj is null)
    {
      throw new ArgumentNullException(message ?? "Value is null");
    }
    return obj;
  }

  public static T NotNull<T>(this T? obj, string? message = null)
    where T : struct
  {
    if (obj is null)
    {
      throw new ArgumentNullException(message ?? "Value is null");
    }
    return obj.Value;
  }
}
