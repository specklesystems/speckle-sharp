using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Speckle.Core.Transports
{
  /// <summary>
  /// Interface defining the contract for transport implementations.
  /// TODO: This is not its final form yet. We need to clean it up and "regularise" it a bit. Add some best practices too. Defs in the implementations.
  /// </summary>
  public interface ITransport
  {
    public string TransportName { get; set; }


    public Action<string, int> OnProgressAction { get; set; }

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
