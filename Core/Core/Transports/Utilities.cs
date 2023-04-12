using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Core.Transports
{
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
        {
          await Task.Delay(frequency);
        }
      });

      if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
      {
        throw new SpeckleException("Process timed out", new TimeoutException());
      }
    }
  }
}
