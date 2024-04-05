using System;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Utils;

// POC: consider wisdom of static
public static class SpeckleTopLevelExceptionHandler
{
  public static async Task Run(
    Func<Task> run,
    Func<TypeLoadException, bool>? typeLoadError = null,
    Func<SpeckleException, bool>? speckleError = null,
    Func<Exception, bool>? unexpectedError = null,
    Func<Exception, bool>? fatalError = null
  )
  {
    // POC: TL-handler
    try
    {
      await run().ConfigureAwait(false);
    }
    catch (TypeLoadException ex)
    {
      if (typeLoadError == null || !typeLoadError(ex))
      {
        throw;
      }
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
