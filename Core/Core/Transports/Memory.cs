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
public sealed class MemoryTransport : ITransport, ICloneable
{
  public IDictionary<string, string> Objects { get; }

  public MemoryTransport() : this(new Dictionary<string, string>()) { }

  public MemoryTransport(IDictionary<string, string> objects)
  {
    Objects = objects;
    SpeckleLog.Logger.Debug("Creating a new Memory Transport");
  }

  public object Clone()
  {
    return new MemoryTransport(Objects)
    {
      TransportName = TransportName,
      OnErrorAction = OnErrorAction,
      OnProgressAction = OnProgressAction,
      CancellationToken = CancellationToken,
      SavedObjectCount = SavedObjectCount
    };
  }

  public CancellationToken CancellationToken { get; set; }

  public string TransportName { get; set; } = "Memory";

  public Action<string, int> OnProgressAction { get; set; }

  [Obsolete("Transports will now throw exceptions")]
  public Action<string, Exception> OnErrorAction { get; set; }

  public int SavedObjectCount { get; private set; }

  public Dictionary<string, object> TransportContext => new() { { "name", TransportName }, { "type", GetType().Name } };

  public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

  public void BeginWrite()
  {
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public void SaveObject(string id, string serializedObject)
  {
    var stopwatch = Stopwatch.StartNew();
    if (CancellationToken.IsCancellationRequested)
      return; // Check for cancellation

    Objects[id] = serializedObject;

    SavedObjectCount++;
    OnProgressAction?.Invoke(TransportName, 1);
    stopwatch.Stop();
    Elapsed += stopwatch.Elapsed;
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    CancellationToken.ThrowIfCancellationRequested();

    var serializedObject = sourceTransport.GetObject(id);

    if (serializedObject is null)
      throw new TransportException(
        this,
        $"Cannot copy {id} from {sourceTransport.TransportName} to {TransportName} as source returned null"
      );

    SaveObject(id, serializedObject);
  }

  public string GetObject(string id)
  {
    var stopwatch = Stopwatch.StartNew();
    var ret = Objects.TryGetValue(id, out string o) ? o : null;
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

  public async Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
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
