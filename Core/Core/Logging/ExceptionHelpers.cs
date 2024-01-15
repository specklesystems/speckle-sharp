using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Speckle.Core.Logging;

public static class ExceptionHelpers
{
  /// <summary>
  /// Helper function for catch blocks to avoid catching and handling/wrapping of some critical exception types that are unlikely to be truly handleable
  /// </summary>
  /// <remarks>
  /// We should aim to always catch specific exception types, and have all functions document the types they may throw.
  /// However, this is not always achievable.
  /// e.g. when dealing with legacy code, some third-party APIs, or in cases where we want to prevent a host app crash.
  /// In these cases, we often want to catch all exceptions, and opt out only of the ones that definitely shouldn't be handled
  /// </remarks>
  /// <example>
  /// <code>
  /// try
  /// {
  ///     SomethingSketchy();
  /// }
  /// catch (Exception ex) when (!IsFatal(ex))
  /// {
  ///    throw new SpeckleException("Failed to do something", ex);
  /// }
  /// </code>
  /// </example>
  /// <param name="ex"></param>
  /// <returns><see langword="true"/> for types that are unlikely to ever be recoverable</returns>
  [Pure]
  public static bool IsFatal(this Exception ex)
  {
    return ex switch
    {
      OutOfMemoryException
      or ThreadAbortException
      or InvalidProgramException
      or AccessViolationException
      or AppDomainUnloadedException
      or BadImageFormatException
        => true,
      _ => false,
    };
  }
}
