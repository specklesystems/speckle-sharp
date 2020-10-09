using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
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

      // TODO

      return parent;
    }
  }
}
