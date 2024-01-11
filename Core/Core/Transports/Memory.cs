using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

  public MemoryTransport()
    : this(new Dictionary<string, string>()) { }

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
      OnProgressAction = OnProgressAction,
      CancellationToken = CancellationToken,
      SavedObjectCount = SavedObjectCount
    };
  }

  public CancellationToken CancellationToken { get; set; }

  public string TransportName { get; set; } = "Memory";

  public Action<string, int>? OnProgressAction { get; set; }

  public int SavedObjectCount { get; private set; }

  public Dictionary<string, object> TransportContext => new() { { "name", TransportName }, { "type", GetType().Name } };

  public TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;

  public void BeginWrite()
  {
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public void SaveObject(string id, string serializedObject)
  {
    CancellationToken.ThrowIfCancellationRequested();
    var stopwatch = Stopwatch.StartNew();

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
    {
      throw new TransportException(
        this,
        $"Cannot copy {id} from {sourceTransport.TransportName} to {TransportName} as source returned null"
      );
    }

    SaveObject(id, serializedObject);
  }

  public string? GetObject(string id)
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
    Action<int>? onTotalChildrenCountKnown = null
  )
  {
    string res = TransportHelpers.CopyObjectAndChildrenSync(
      id,
      this,
      targetTransport,
      onTotalChildrenCountKnown,
      CancellationToken
    );
    return Task.FromResult(res);
  }

  public Task WriteComplete()
  {
    return Task.CompletedTask;
  }

  public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
  {
    Dictionary<string, bool> ret = new(objectIds.Count);
    foreach (string objectId in objectIds)
    {
      ret[objectId] = Objects.ContainsKey(objectId);
    }

    return Task.FromResult(ret);
  }

  [Obsolete("No replacement required, memory transport is always sync")]
  [SuppressMessage("Design", "CA1024:Use properties where appropriate")]
  public bool GetWriteCompletionStatus()
  {
    return true; // can safely assume it's always true, as ops are atomic?
  }

  public override string ToString()
  {
    return $"Memory Transport {TransportName}";
  }

  [Obsolete("Transports will now throw exceptions", true)]
  public Action<string, Exception>? OnErrorAction { get; set; }
}
