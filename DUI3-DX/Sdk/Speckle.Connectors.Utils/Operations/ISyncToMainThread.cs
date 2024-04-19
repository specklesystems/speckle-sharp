using System;
using System.Threading.Tasks;

namespace Speckle.Connectors.Utils.Operations;

public interface ISyncToMainThread
{
  public Task<T> RunOnThread<T>(Func<T> func);
}
