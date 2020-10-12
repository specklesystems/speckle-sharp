using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Speckle.Core.Transports
{
  /// <summary>
  /// An in memory storage of speckle objects.
  /// </summary>
  public class MemoryTransport : ITransport
  {
    public Dictionary<string, string> Objects;

    public string TransportName { get; set; } = "Memory";

    public Action<string, int> OnProgressAction { get; set; }

    public MemoryTransport()
    {
      Log.AddBreadcrumb("New Memory Transport");

      Objects = new Dictionary<string, string>();
    }

    public void SaveObject(string hash, string serializedObject)
    {
      Objects[hash] = serializedObject;
    }

    public void SaveObject(string id, ITransport sourceTransport)
    {
      throw new NotImplementedException();
    }

    public string GetObject(string hash)
    {
      if (Objects.ContainsKey(hash)) return Objects[hash];
      else
      {
        Log.CaptureException(new SpeckleException("No object found in this memory transport."), level: Sentry.Protocol.SentryLevel.Warning);
        throw new SpeckleException("No object found in this memory transport.");
      }
    }

    public Task<string> CopyObjectAndChildren(string id, ITransport targetTransport)
    {
      throw new NotImplementedException();
    }

    public bool GetWriteCompletionStatus()
    {
      return true; // can safely assume it's always true, as ops are atomic?
    }

    public Task WriteComplete()
    {
      return Utilities.WaitUntil(() => true);
    }

    public override string ToString()
    {
      return $"Memory Transport {TransportName}";
    }
  }

}
