using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Speckle.Models;

namespace Speckle.Transports
{

  public class MagicTransport : ITransport
  {

    public MemoryTransport MemoryTransport;
    public DiskTransport DiskTransport;

    public IEnumerable<string> GetAllObjects()
    {
      throw new NotImplementedException();
    }

    public string GetObject(string hash)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<string> GetObjects(IEnumerable<string> hashes)
    {
      throw new NotImplementedException();
    }

    public void SaveObject(string hash, string serializedObject)
    {
      throw new NotImplementedException();
    }

    public void SaveObjects(Dictionary<string, string> objects)
    {
      throw new NotImplementedException();
    }
  }

  public class MemoryTransport : ITransport
  {
    public Dictionary<string, string> Objects;

    public MemoryTransport()
    {
      Objects = new Dictionary<string, string>();
    }

    public void SaveObject(string hash, string serializedObject)
    {
      Objects[hash] = serializedObject;
    }

    public void SaveObjects(Dictionary<string, string> objects)
    {
      foreach (var kvp in objects)
        Objects[kvp.Key] = kvp.Value;
    }

    public string GetObject(string hash)
    {
      if (Objects.ContainsKey(hash)) return Objects[hash];
      else
        throw new Exception("No object found in this memory transport.");
    }

    public IEnumerable<string> GetObjects(IEnumerable<string> hashes)
    {
      foreach (var id in hashes)
      {
        if (Objects.ContainsKey(id))
          yield return Objects[id];
      }
    }

    public IEnumerable<string> GetAllObjects()
    {
      foreach (var kvp in Objects)
        yield return kvp.Value;
    }
  }

  public class DiskTransport : ITransport
  {
    public string RootPath { get; set; }

    /// <summary>
    /// Creates a transport that writes to disk, in the specified file path. Files are saved in folders created from the first two chars of the hash.
    /// </summary>
    /// <param name="path">If left null, defaults to the a "SpeckleObjectCache" folder in the current environment's ApplicationData location.</param>
    public DiskTransport(string path = null)
    {
      if (path == null)
        RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpeckleObjectCache");

      Directory.CreateDirectory(RootPath);
    }

    public string GetObject(string hash)
    {
      throw new NotImplementedException();
    }

    public void SaveObject(string hash, string serializedObject)
    {
      var (dirPath, filePath) = DirFileFromHash(hash);

      if (File.Exists(filePath)) return;
      if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

      File.WriteAllText(filePath, serializedObject);
    }

    (string, string) DirFileFromHash(string hash)
    {
      var subFolder = hash.Substring(0, 2);
      var filename = hash.Substring(2);

      return (Path.Combine(RootPath, subFolder), Path.Combine(RootPath, subFolder, filename));
    }
  }
}
