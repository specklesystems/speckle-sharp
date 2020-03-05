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
    public void SaveObject(string hash, string serializedObject);

    public string GetObject(string hash);
  }
}
