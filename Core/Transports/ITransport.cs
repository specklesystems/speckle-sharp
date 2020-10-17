using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Speckle.Core.Transports
{
  /// <summary>
  /// Interface defining the contract for transport implementations.
  /// </summary>
  public interface ITransport
  {
    public string TransportName { get; set; }

    /// <summary>
    /// Should be checked often and gracefully stop all in progress sending if requested.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Used to report progress during the transport's longer operations.
    /// </summary>
    public Action<string, int> OnProgressAction { get; set; }

    /// <summary>
    /// Used to report errors during the transport's longer operations.
    /// </summary>
    public Action<string, Exception> OnErrorAction { get; set; }

    /// <summary>
    /// Saves an object.
    /// </summary>
    /// <param name="id">The hash of the object.</param>
    /// <param name="serializedObject">The full string representation of the object.</param>
    public void SaveObject(string id, string serializedObject);

    /// <summary>
    /// Saves an object, retrieveing its serialised version from the provided transport. 
    /// </summary>
    /// <param name="id">The hash of the object.</param>
    /// <param name="sourceTransport">The transport from where to retrieve it.</param>
    public void SaveObject(string id, ITransport sourceTransport);

    /// <summary>
    /// Awaitable method to figure out whether writing is completed. 
    /// </summary>
    /// <returns></returns>
    public Task WriteComplete();

    /// <summary>
    /// Gets an object.
    /// </summary>
    /// <param name="id">The object's hash.</param>
    /// <returns></returns>
    public string GetObject(string id);

    /// <summary>
    /// Copies the parent object and all its children to the provided transport.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>The string representation of the root object.</returns>
    public Task<string> CopyObjectAndChildren(string id, ITransport targetTransport);
  }
}
