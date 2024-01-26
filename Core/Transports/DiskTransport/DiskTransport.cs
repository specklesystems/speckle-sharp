using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Helpers;

namespace Speckle.Core.Transports;

/// <summary>
/// Writes speckle objects to disk.
/// </summary>
public class DiskTransport : ICloneable, ITransport
{
  public DiskTransport(string? basePath = null)
  {
    basePath ??= Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "DiskTransportFiles");

    RootPath = Path.Combine(basePath);

    Directory.CreateDirectory(RootPath);
  }

  public string RootPath { get; set; }

  public object Clone()
  {
    return new DiskTransport
    {
      RootPath = RootPath,
      CancellationToken = CancellationToken,
      OnErrorAction = OnErrorAction,
      OnProgressAction = OnProgressAction,
      TransportName = TransportName
    };
  }

  public string TransportName { get; set; } = "Disk";

  public Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "basePath", RootPath }
    };

  public CancellationToken CancellationToken { get; set; }

  public Action<string, int>? OnProgressAction { get; set; }

  public Action<string, Exception>? OnErrorAction { get; set; }

  public int SavedObjectCount { get; private set; }

  public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

  public void BeginWrite()
  {
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public string? GetObject(string id)
  {
    CancellationToken.ThrowIfCancellationRequested();

    var filePath = Path.Combine(RootPath, id);
    if (File.Exists(filePath))
    {
      return File.ReadAllText(filePath, Encoding.UTF8);
    }

    return null;
  }

  public void SaveObject(string id, string serializedObject)
  {
    var stopwatch = Stopwatch.StartNew();
    CancellationToken.ThrowIfCancellationRequested();

    var filePath = Path.Combine(RootPath, id);
    if (File.Exists(filePath))
    {
      return;
    }

    try
    {
      File.WriteAllText(filePath, serializedObject, Encoding.UTF8);
    }
    catch (Exception ex)
    {
      throw new TransportException(this, $"Failed to write object {id} to disk", ex);
    }

    SavedObjectCount++;
    OnProgressAction?.Invoke(TransportName, SavedObjectCount);
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

  public Task WriteComplete()
  {
    return Task.CompletedTask;
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

  public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds)
  {
    Dictionary<string, bool> ret = new();
    foreach (string objectId in objectIds)
    {
      var filePath = Path.Combine(RootPath, objectId);
      ret[objectId] = File.Exists(filePath);
    }
    return Task.FromResult(ret);
  }

  public override string ToString()
  {
    return $"Disk Transport @{RootPath}";
  }
}
