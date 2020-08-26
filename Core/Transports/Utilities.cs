using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Speckle.Core.Transports
{
  public static class Utilities
  {
    /// <summary>
    /// Chunks a list into pieces.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="chunkSize"></param>
    /// <returns></returns>
    public static IEnumerable<List<T>> SplitList<T>(List<T> list, int chunkSize = 50)
    {
      for (int i = 0; i < list.Count; i += chunkSize)
      {
        yield return list.GetRange(i, Math.Min(chunkSize, list.Count - i));
      }
    }

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
        while (!condition()) await Task.Delay(frequency);
      });

      if (waitTask != await Task.WhenAny(waitTask,
              Task.Delay(timeout)))
        Log.CaptureAndThrow(new TimeoutException());
    }

  }
}
