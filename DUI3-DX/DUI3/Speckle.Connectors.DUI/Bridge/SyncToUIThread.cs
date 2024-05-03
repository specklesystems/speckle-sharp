using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Speckle.Connectors.Utils.Operations;
using Speckle.Core.Logging;

namespace Speckle.Connectors.DUI.Bridge;

public class SyncToUIThread : ISyncToMainThread
{
  private readonly IBridge _bridge;

  public SyncToUIThread(IBridge bridge)
  {
    _bridge = bridge;
  }

  [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Task Completion Source")]
  public Task<T> RunOnThread<T>(Func<T> func)
  {
    TaskCompletionSource<T> tcs = new();

    _bridge.RunOnMainThread(() =>
    {
      try
      {
        T result = func.Invoke();
        tcs.SetResult(result);
      }
      catch (Exception ex)
      {
        tcs.SetException(ex);
      }
    });

    return tcs.Task;
  }
}
