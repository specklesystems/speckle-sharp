using System;
using System.Diagnostics;

namespace Speckle.Core.Logging;

internal static class LoggingHelpers
{
  private const long TICKS_PER_MILLISECOND = 10000;
  private const long TICKS_PER_SECOND = TICKS_PER_MILLISECOND * 1000;
  private static readonly double s_sTickFrequency = (double)TICKS_PER_SECOND / Stopwatch.Frequency;

  public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
  {
    return new TimeSpan((long)((endingTimestamp - startingTimestamp) * s_sTickFrequency));
  }
}
