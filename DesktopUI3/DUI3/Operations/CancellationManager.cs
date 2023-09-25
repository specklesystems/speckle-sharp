using System.Collections.Generic;
using System.Threading;

namespace DUI3.Operations;

/// <summary>
/// Provides a mechanism for cancelling operations.
/// </summary>
public interface ICancellable
{
  public CancellationManager CancellationManager { get; }
}

/// <summary>
/// Util class to manage cancellations.
/// </summary>
public class CancellationManager
{
  /// <summary>
  /// Dictionary to relate <see cref="CancellationTokenSource"/> with registered id.
  /// </summary>
  private Dictionary<string, CancellationTokenSource> operationsInProgress = new();

  /// <summary>
  /// Get token with registered id.
  /// </summary>
  /// <param name="id"> Id of the operation.</param>
  /// <returns> CancellationToken that belongs to operation.</returns>
  public CancellationToken GetToken(string id)
  {
    return operationsInProgress[id].Token;
  }

  /// <summary>
  /// Whether given id registered or not.
  /// </summary>
  /// <param name="id"> Id to check registration.</param>
  /// <returns> Whether given id registered or not.</returns>
  public bool IsExist(string id)
  {
    return operationsInProgress.ContainsKey(id);
  }

  /// <summary>
  /// Initialize a token source for cancellable operation.
  /// </summary>
  /// <param name="id"> Id to register token.</param>
  /// <returns> Initialized cancellation token source.</returns>
  public CancellationTokenSource InitCancellationTokenSource(string id)
  {
    var cts = new CancellationTokenSource();
    operationsInProgress[id] = cts;
    return cts;
  }
  
  /// <summary>
  /// Cancel operation.
  /// </summary>
  /// <param name="id">Id to cancel operation.</param>
  public void CancelOperation(string id)
  {
    operationsInProgress[id].Cancel();
    operationsInProgress[id].Dispose();
    operationsInProgress.Remove(id);
  }

  /// <summary>
  /// Whether cancellation requested already or not.
  /// </summary>
  /// <param name="id"> Id to check cancellation requested already or not.</param>
  /// <returns></returns>
  public bool IsCancellationRequested(string id)
  {
    return operationsInProgress[id].IsCancellationRequested;
  }
}
