using System;
using System.Runtime.CompilerServices;
using Serilog;

namespace RevitSharedResources.Extensions.SpeckleExtensions;

public static class ILoggerExtensions
{
  public static void LogDefaultError(this ILogger logger, Exception ex, [CallerMemberName] string caller = null)
  {
    logger.Error(ex, "Method named {caller} threw an error of type {type}", caller, ex.GetType());
  }
}
