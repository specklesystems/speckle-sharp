namespace Speckle.Connectors.Utils.Cancellation;

/// <summary>
/// Util class to manage cancellations.
/// </summary>
public class CancellationManager
{
  /// <summary>
  /// Dictionary to relate <see cref="CancellationTokenSource"/> with registered id.
  /// </summary>
  private readonly Dictionary<string, CancellationTokenSource> _operationsInProgress = new();

  public int NumberOfOperations => _operationsInProgress.Count;

  /// <summary>
  /// Get token with registered id.
  /// </summary>
  /// <param name="id"> Id of the operation.</param>
  /// <returns> CancellationToken that belongs to operation.</returns>
  public CancellationToken GetToken(string id)
  {
    return _operationsInProgress[id].Token;
  }

  /// <summary>
  /// Whether given id registered or not.
  /// </summary>
  /// <param name="id"> Id to check registration.</param>
  /// <returns> Whether given id registered or not.</returns>
  public bool IsExist(string id)
  {
    return _operationsInProgress.ContainsKey(id);
  }

  public void CancelAllOperations()
  {
    foreach (var operation in _operationsInProgress)
    {
      operation.Value.Cancel();
      operation.Value.Dispose();
    }
    _operationsInProgress.Clear();
  }

  /// <summary>
  /// Initialize a token source for cancellable operation.
  /// </summary>
  /// <param name="id"> Id to register token.</param>
  /// <returns> Initialized cancellation token source.</returns>
  public CancellationTokenSource InitCancellationTokenSource(string id)
  {
    if (IsExist(id))
    {
      CancelOperation(id);
    }

    var cts = new CancellationTokenSource();
    _operationsInProgress[id] = cts;
    return cts;
  }

  /// <summary>
  /// Cancel operation.
  /// </summary>
  /// <param name="id">Id to cancel operation.</param>
  public void CancelOperation(string id)
  {
    if (_operationsInProgress.TryGetValue(id, out CancellationTokenSource cts))
    {
      cts.Cancel();
      cts.Dispose();
      _operationsInProgress.Remove(id);
    }
  }

  /// <summary>
  /// Whether cancellation requested already or not.
  /// </summary>
  /// <param name="id"> Id to check cancellation requested already or not.</param>
  /// <returns></returns>
  public bool IsCancellationRequested(string id)
  {
    return _operationsInProgress[id].IsCancellationRequested;
  }
}
