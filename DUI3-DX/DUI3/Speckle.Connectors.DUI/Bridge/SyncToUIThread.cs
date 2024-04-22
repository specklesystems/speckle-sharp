using System;
using System.Threading.Tasks;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.DUI.Bridge;

public class SyncToUIThread : ISyncToMainThread
{
  private readonly IBridge _bridge;

  public SyncToUIThread(IBridge bridge)
  {
    _bridge = bridge;
  }

  public Task<T> RunOnThread<T>(Func<T> func)
  {
    TaskCompletionSource<T> tcs = new();

    _bridge.RunOnMainThread(() =>
    {
      T result = func.Invoke();
      tcs.SetResult(result);
    });

    return tcs.Task;
  }
}
