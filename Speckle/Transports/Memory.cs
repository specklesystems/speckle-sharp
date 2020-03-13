using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Speckle.Models;

namespace Speckle.Transports
{

  /// <summary>
  /// An in memory storage of speckle objects.
  /// </summary>
  public class MemoryTransport : ITransport
  {
    public Dictionary<string, string> Objects;

    public string TransportName { get; set; } = "Memory";

    public MemoryTransport()
    {
      Objects = new Dictionary<string, string>();
    }

    public void SaveObject(string hash, string serializedObject, bool overwrite = false)
    {
      Objects[hash] = serializedObject;
    }

    public string GetObject(string hash)
    {
      if (Objects.ContainsKey(hash)) return Objects[hash];
      else
        throw new Exception("No object found in this memory transport.");
    }
  }

}
