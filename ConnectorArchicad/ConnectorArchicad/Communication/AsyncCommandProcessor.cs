using System;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Archicad.Communication
{
  internal class AsyncCommandProcessor
  {
    #region --- Fields ---

    public static AsyncCommandProcessor Instance { get; } = new AsyncCommandProcessor();

    #endregion

    #region --- Functions ---

    public static Task<TResult>? Execute<TResult>(Commands.ICommand<TResult> command, CumulativeTimer cumulativeTimer = null) where TResult : class
    {
      return Execute(command, CancellationToken.None, cumulativeTimer);
    }

    public static Task<TResult>? Execute<TResult>(Commands.ICommand<TResult> command, CancellationToken token, CumulativeTimer cumulativeTimer) where TResult : class
    {
      try
      {
        return Task.Run(() => command.Execute(cumulativeTimer), token);
      }
      catch (Exception e)
      {
        return null;
      }
    }

    #endregion
  }
}
