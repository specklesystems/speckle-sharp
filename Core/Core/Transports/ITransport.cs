using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Models;

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
    /// Optional: signals to the transport that writes are about to begin.
    /// </summary>
    public void BeginWrite();

    /// <summary>
    /// Optional: signals to the transport that no more items will need to be written.
    /// </summary>
    public void EndWrite();

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
    /// <param name="id">The id of the object you want to copy.</param>
    /// <param name="targetTransport">The transport you want to copy the object to.</param>
    /// <param name="onTotalChildrenCountKnown">(Optional) an action that will be invoked once, when the amount of object children to be copied over is known.</param>
    /// <returns>The string representation of the root object.</returns>
    public Task<string> CopyObjectAndChildren(string id, ITransport targetTransport, Action<int> onTotalChildrenCountKnown = null);

    /// <summary>
    /// Checks if objects are present in the transport
    /// </summary>
    /// <param name="objectIds">List of object ids to check</param>
    /// <returns>A dictionary with the specified object ids as keys and boolean values, whether each object is present in the transport or not</returns>
    public Task<Dictionary<string, bool>> HasObjects(List<string> objectIds);
  }

  public interface IBlobCapableTransport
  {
    public string BlobStorageFolder { get; }

    public void SaveBlob(Blob obj);

    // NOTE: not needed, should be implemented in "CopyObjectsAndChildren"
    //public void GetBlob(Blob obj);
  }
}
