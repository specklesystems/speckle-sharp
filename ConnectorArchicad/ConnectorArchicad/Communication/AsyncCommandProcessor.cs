using System;
using System.Threading;
using System.Threading.Tasks;

namespace Archicad.Communication
{
  internal class AsyncCommandProcessor
  {
    #region --- Fields ---

    public static AsyncCommandProcessor Instance { get; } = new AsyncCommandProcessor();

    #endregion

    #region --- Functions ---

    public static Task<TResult>? Execute<TResult>(Commands.ICommand<TResult> command) where TResult : class
    {
      return Execute(command, CancellationToken.None);
    }

    public static Task<TResult>? Execute<TResult>(Commands.ICommand<TResult> command, CancellationToken token) where TResult : class
    {
      try
      {
        return Task.Run(command.Execute, token);
      }
      catch (Exception e)
      {
        return null;
      }
    }

    #endregion
  }
}
