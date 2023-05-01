using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Logging;

namespace Speckle.Core.Transports;

/// <summary>
/// An in memory storage of speckle objects.
/// </summary>
public class MemoryTransport : ITransport, IDisposable, ICloneable
{
  public Dictionary<string, string> Objects;

  public MemoryTransport()
  {
    SpeckleLog.Logger.Debug("Creating a new Memory Transport");

    Objects = new Dictionary<string, string>();
  }

  public object Clone()
  {
    return new MemoryTransport
    {
      TransportName = TransportName,
      OnErrorAction = OnErrorAction,
      OnProgressAction = OnProgressAction,
      CancellationToken = CancellationToken,
      Objects = Objects,
      SavedObjectCount = SavedObjectCount
    };
  }

  public void Dispose()
  {
    Objects = null;
    OnErrorAction = null;
    OnProgressAction = null;
    SavedObjectCount = 0;
  }

  public CancellationToken CancellationToken { get; set; }

  public string TransportName { get; set; } = "Memory";

  public Action<string, int> OnProgressAction { get; set; }

  public Action<string, Exception> OnErrorAction { get; set; }

  public int SavedObjectCount { get; set; }

  public Dictionary<string, object> TransportContext => new() { { "name", TransportName }, { "type", GetType().Name } };

  public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

  public void BeginWrite()
  {
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public void SaveObject(string hash, string serializedObject)
  {
    var stopwatch = Stopwatch.StartNew();
    if (CancellationToken.IsCancellationRequested)
      return; // Check for cancellation

    Objects[hash] = serializedObject;

    SavedObjectCount++;
    OnProgressAction?.Invoke(TransportName, 1);
    stopwatch.Stop();
    Elapsed += stopwatch.Elapsed;
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    throw new NotImplementedException();
  }

  public string GetObject(string hash)
  {
    var stopwatch = Stopwatch.StartNew();
    var ret = Objects.ContainsKey(hash) ? Objects[hash] : null;
    stopwatch.Stop();
    Elapsed += stopwatch.Elapsed;
    return ret;
  }

  public Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int> onTotalChildrenCountKnown = null
  )
  {
    throw new NotImplementedException();
  }

  public Task WriteComplete()
  {
    return Utilities.WaitUntil(() => true);
  }

  public async Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
  {
    Dictionary<string, bool> ret = new();
    foreach (string objectId in objectIds)
      ret[objectId] = Objects.ContainsKey(objectId);

    return ret;
  }

  public bool GetWriteCompletionStatus()
  {
    return true; // can safely assume it's always true, as ops are atomic?
  }

  public override string ToString()
  {
    return $"Memory Transport {TransportName}";
  }
}
