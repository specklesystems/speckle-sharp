using System;
using System.Collections.Generic;

namespace Speckle.Autofac;

public static class EnumerationExtensions
{
  public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
  {
    foreach (var item in items)
    {
      action(item);
    }
  }
}
