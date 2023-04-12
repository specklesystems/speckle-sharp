using System;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Speckle.Core.Transports;

public static class Utilities
{
  /// <summary>
  /// Waits until the provided function returns true.
  /// </summary>
  /// <param name="condition"></param>
  /// <param name="frequency"></param>
  /// <param name="timeout"></param>
  /// <returns></returns>
  public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
  {
    var waitTask = Task.Run(async () =>
    {
      while (!condition())
        await Task.Delay(frequency).ConfigureAwait(false);
    });

    if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)).ConfigureAwait(false))
      throw new SpeckleException("Process timed out", new TimeoutException());
  }
}
