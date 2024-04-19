using System;
using System.Threading.Tasks;
using Revit.Async;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.Revit.Operations.Receive;

internal class RevitContextAccessor : ISyncToMainThread
{
  public Task<T> RunOnThread<T>(Func<T> func) => RevitTask.RunAsync(func);
}
