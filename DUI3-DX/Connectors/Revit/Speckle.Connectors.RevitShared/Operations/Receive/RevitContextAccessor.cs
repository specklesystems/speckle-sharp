using Revit.Async;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Revit.Operations.Receive;

internal class RevitContextAccessor : ISyncToThread
{
  public Task<T> RunOnThread<T>(Func<T> func) => RevitTask.RunAsync(func);
}
