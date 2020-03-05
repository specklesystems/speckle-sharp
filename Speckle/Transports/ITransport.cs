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
    /// <summary>
    /// Saves an object.
    /// </summary>
    /// <param name="hash">The hash of the object.</param>
    /// <param name="serializedObject">The full string representation of the object.</param>
    public void SaveObject(string hash, string serializedObject);

    /// <summary>
    /// Gets an object.
    /// </summary>
    /// <param name="hash">The object's hash.</param>
    /// <returns></returns>
    public string GetObject(string hash);
  }
}
