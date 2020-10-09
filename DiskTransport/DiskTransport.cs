using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Speckle.Core.Transports;

namespace DiskTransport
{
  /// <summary>
  /// Warning! Untested.
  /// </summary>
  public class DiskTransport : ITransport
  {
    public string TransportName { get; set; } = "Disk";

    public Action<string, int> OnProgressAction { get; set; }

    public string RootPath { get; set; }

    public DiskTransport(string basePath, string applicationName = ".spk", string scope = "objects")
    {
      if (basePath == null)
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      RootPath = Path.Combine(basePath, applicationName, scope);

      Directory.CreateDirectory(RootPath);
    }

    public string GetObject(string id)
    {
      var filePath = Path.Combine(RootPath, id);
      if (File.Exists(filePath))
      {
        return File.ReadAllText(filePath, Encoding.UTF8);
      }

      throw new Exception($"Could not find the specified object ({filePath}).");
    }

    public void SaveObject(string id, string serializedObject)
    {
      var filePath = Path.Combine(RootPath, id);
      if (File.Exists(filePath)) return;

      File.WriteAllText(filePath, serializedObject, Encoding.UTF8);
    }

    public void SaveObject(string id, ITransport sourceTransport)
    {
      var serializedObject = sourceTransport.GetObject(id);
      SaveObject(id, serializedObject);
    }

    public async Task WriteComplete()
    {
      return;
    }

    public async Task<string> CopyObjectAndChildren(string id, ITransport targetTransport)
    {
      var parent = GetObject(id);
      
      targetTransport.SaveObject(id, parent);
      
      var partial = JsonConvert.DeserializeObject<Placeholder>(parent);

      if (partial.__closure == null || partial.__closure.Count == 0) return parent;
      
      int i = 0;
      foreach(var childId in partial.__closure)
      {
        var child = GetObject(childId);
        targetTransport.SaveObject(childId, child);
        OnProgressAction?.Invoke($"{TransportName}", i++);
      }

      return parent;
    }

    class Placeholder
    {
      public List<string> __closure { get; set; } = new List<string>();
    }
  }
}
