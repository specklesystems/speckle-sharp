namespace Speckle.Core.Transports;

public static class TransportExtensions
{
  /// <summary>
  /// <inheritdoc cref="ITransport.SaveObject(string, string)"/>
  /// Retrieves the object from the provided transport
  /// </summary>
  /// <param name="destination">The destination</param>
  /// <param name="id"><inheritdoc cref="ITransport.SaveObject(string, string)"/></param>
  /// <param name="source">The transport from where to retrieve it.</param>
  /// <exception cref="TransportException">Failed to save object</exception>
  /// <exception cref="System.OperationCanceledException"><see cref="System.Threading.CancellationToken"/> requested cancel</exception>
  public static void SaveObject(this ITransport destination, string id, ITransport source)
  {
    string? objectData = source.GetObject(id);

    if (objectData is null)
    {
      throw new TransportException(
        destination,
        $"Cannot copy {id} from {source.TransportName} to {destination.TransportName} as source returned null"
      );
    }

    destination.SaveObject(id, objectData);
  }
}
