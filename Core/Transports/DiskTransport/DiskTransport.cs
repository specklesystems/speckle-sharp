using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;
using Speckle.Core.Transports;

namespace DiskTransport
{
  /// <summary>
  /// Writes speckle objects to disk.
  /// </summary>
  public class DiskTransport : ITransport
  {
    public string TransportName { get; set; } = "Disk";

    public CancellationToken CancellationToken { get; set; }

    public Action<string, int> OnProgressAction { get; set; }

    public Action<string, Exception> OnErrorAction { get; set; }

    public string RootPath { get; set; }

    public int SavedObjectCount { get; private set; } = 0;

    public DiskTransport(string basePath = null)
    {
      if (basePath == null)
        basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle", "DiskTransportFiles");

      RootPath = Path.Combine(basePath);

      Directory.CreateDirectory(RootPath);
    }

    public void BeginWrite()
    {
      SavedObjectCount = 0;
    }

    public void EndWrite() { }

    public string GetObject(string id)
    {
      if (CancellationToken.IsCancellationRequested) return null; // Check for cancellation

      var filePath = Path.Combine(RootPath, id);
      if (File.Exists(filePath))
      {
        return File.ReadAllText(filePath, Encoding.UTF8);
      }

      throw new Exception($"Could not find the specified object ({filePath}).");
    }

    public void SaveObject(string id, string serializedObject)
    {
      if (CancellationToken.IsCancellationRequested) return; // Check for cancellation

      var filePath = Path.Combine(RootPath, id);
      if (File.Exists(filePath)) return;

      File.WriteAllText(filePath, serializedObject, Encoding.UTF8);
      SavedObjectCount++;
      OnProgressAction?.Invoke(TransportName, SavedObjectCount);
    }

    public void SaveObject(string id, ITransport sourceTransport)
    {
      if (CancellationToken.IsCancellationRequested) return; // Check for cancellation

      var serializedObject = sourceTransport.GetObject(id);
      SaveObject(id, serializedObject);
    }

    public async Task WriteComplete()
    {
      return;
    }

    public async Task<string> CopyObjectAndChildren(string id, ITransport targetTransport, Action<int> onTotalChildrenCountKnown = null)
    {
      if (CancellationToken.IsCancellationRequested) return null; // Check for cancellation

      var parent = GetObject(id);

      targetTransport.SaveObject(id, parent);

      var partial = JsonConvert.DeserializeObject<Placeholder>(parent);

      if (partial.__closure == null || partial.__closure.Count == 0) return parent;

      int i = 0;
      foreach (var kvp in partial.__closure)
      {
        if (CancellationToken.IsCancellationRequested) return null; // Check for cancellation

        var child = GetObject(kvp.Key);
        targetTransport.SaveObject(kvp.Key, child);
        OnProgressAction?.Invoke($"{TransportName}", i++);
      }

      return parent;
    }

    public override string ToString()
    {
      return $"Disk Transport @{RootPath}";
    }

    class Placeholder
    {
      public Dictionary<string, int> __closure { get; set; } = new Dictionary<string, int>();
    }
  }
}
