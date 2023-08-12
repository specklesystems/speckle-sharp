using System;
using System.Diagnostics;

namespace Speckle.Core.Logging;

public static class LoggingHelpers
{
  private const long TicksPerMillisecond = 10000;
  private const long TicksPerSecond = TicksPerMillisecond * 1000;
  private static readonly double STickFrequency = (double)TicksPerSecond / Stopwatch.Frequency;

  public static TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
  {
    return new TimeSpan((long)((endingTimestamp - startingTimestamp) * STickFrequency));
  }
}
