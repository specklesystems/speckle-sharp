using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Speckle.Transports
{
  /// <summary>
  /// Interface defining the contract for transport implementations.
  /// </summary>
  public interface ITransport
  {
    public string TransportName { get; set; }

    /// <summary>
    /// Saves an object.
    /// </summary>
    /// <param name="id">The hash of the object.</param>
    /// <param name="serializedObject">The full string representation of the object.</param>
    /// <param name="overwrite">If true, will overrwrite the file even if present.</param>
    public void SaveObject(string id, string serializedObject);

    /// <summary>
    /// Gets an object.
    /// </summary>
    /// <param name="id">The object's hash.</param>
    /// <returns></returns>
    public string GetObject(string id);

    public bool GetWriteCompletionStatus();

    public Task WriteComplete();
  }

  public interface IRemoteTransport : ITransport
  {
    /// <summary>
    /// Should get an object with all of its children from a remote, and store them locally.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public Task<string> GetObjectAndChildren(string hash);
  }
}
