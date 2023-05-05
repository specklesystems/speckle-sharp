using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Helpers;
using Speckle.Core.Transports;
using Speckle.Newtonsoft.Json;

namespace DiskTransport;

/// <summary>
/// Writes speckle objects to disk.
/// </summary>
public class DiskTransport : ICloneable, ITransport
{
  public DiskTransport(string basePath = null)
  {
    if (basePath == null)
      basePath = Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "DiskTransportFiles");

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
    new() { { "name", TransportName }, { "type", GetType().Name }, { "basePath", RootPath } };

  public CancellationToken CancellationToken { get; set; }

  public Action<string, int> OnProgressAction { get; set; }

  public Action<string, Exception> OnErrorAction { get; set; }

  public int SavedObjectCount { get; private set; }

  public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

  public void BeginWrite()
  {
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public string GetObject(string id)
  {
    if (CancellationToken.IsCancellationRequested)
      return null; // Check for cancellation

    var filePath = Path.Combine(RootPath, id);
    if (File.Exists(filePath))
      return File.ReadAllText(filePath, Encoding.UTF8);

    return null;
  }

  public void SaveObject(string id, string serializedObject)
  {
    var stopwatch = Stopwatch.StartNew();
    if (CancellationToken.IsCancellationRequested)
      return; // Check for cancellation

    var filePath = Path.Combine(RootPath, id);
    if (File.Exists(filePath))
      return;

    File.WriteAllText(filePath, serializedObject, Encoding.UTF8);
    SavedObjectCount++;
    OnProgressAction?.Invoke(TransportName, SavedObjectCount);
    stopwatch.Stop();
    Elapsed += stopwatch.Elapsed;
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    if (CancellationToken.IsCancellationRequested)
      return; // Check for cancellation

    var serializedObject = sourceTransport.GetObject(id);
    SaveObject(id, serializedObject);
  }

  public async Task WriteComplete() { }

  public async Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int> onTotalChildrenCountKnown = null
  )
  {
    if (CancellationToken.IsCancellationRequested)
      return null; // Check for cancellation

    var parent = GetObject(id);

    targetTransport.SaveObject(id, parent);

    var partial = JsonConvert.DeserializeObject<Placeholder>(parent);

    if (partial.__closure == null || partial.__closure.Count == 0)
      return parent;

    int i = 0;
    foreach (var kvp in partial.__closure)
    {
      if (CancellationToken.IsCancellationRequested)
        return null; // Check for cancellation

      var child = GetObject(kvp.Key);
      targetTransport.SaveObject(kvp.Key, child);
      OnProgressAction?.Invoke($"{TransportName}", i++);
    }

    return parent;
  }

  public async Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
  {
    Dictionary<string, bool> ret = new();
    foreach (string objectId in objectIds)
    {
      var filePath = Path.Combine(RootPath, objectId);
      ret[objectId] = File.Exists(filePath);
    }
    return ret;
  }

  public override string ToString()
  {
    return $"Disk Transport @{RootPath}";
  }

  private class Placeholder
  {
    public Dictionary<string, int> __closure { get; set; } = new();
  }
}
