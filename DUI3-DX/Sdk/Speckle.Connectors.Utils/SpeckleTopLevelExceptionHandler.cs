using System;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Utils;

// POC: consider wisdom of static
public static class SpeckleTopLevelExceptionHandler
{
  // POC: async/await?
  // handlers for
  public static void Run(
    Action run,
    Func<SpeckleException, bool>? speckleError = null,
    Func<Exception, bool>? unexpectedError = null,
    Func<Exception, bool>? fatalError = null
  )
  {
    // POC: TL-handler
    try
    {
      run();
    }
    catch (SpeckleException spex)
    {
      if (speckleError == null || !speckleError(spex))
      {
        throw;
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      if (unexpectedError == null || !unexpectedError(ex))
      {
        throw;
      }
    }
    catch (Exception ex) when (ex.IsFatal())
    {
      if (fatalError == null || !fatalError(ex))
      {
        throw;
      }
    }
  }
}
