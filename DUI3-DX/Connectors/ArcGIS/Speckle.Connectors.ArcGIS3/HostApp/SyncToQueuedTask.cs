using ArcGIS.Desktop.Framework.Threading.Tasks;
using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.ArcGIS.HostApp;

public class SyncToQueuedTask : ISyncToThread
{
  public Task<T> RunOnThread<T>(Func<T> func) => QueuedTask.Run(func);
}
