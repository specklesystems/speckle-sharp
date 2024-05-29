using Speckle.Connectors.Utils.Operations;

namespace Speckle.Connectors.DUI.Bridge;

/// <summary>
/// Implements the <see cref="ISyncToThread"/> interface and runs a given function on the current thread using Task.Run.
/// </summary>
public class SyncToCurrentThread : ISyncToThread
{
  /// <summary>
  /// Executes a given function on the current thread using Task.Run.
  /// </summary>
  /// <typeparam name="T">The return type of the function.</typeparam>
  /// <param name="func">The function to execute.</param>
  /// <returns>A Task object representing the asynchronous operation.</returns>
  public Task<T> RunOnThread<T>(Func<T> func) => Task.FromResult(func.Invoke());
}
