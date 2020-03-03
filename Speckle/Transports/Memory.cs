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

    //public ServerTransport ServerTransport; // TODO! 

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
    public DiskTransport(string path = null)
    {
      if (path == null)
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpeckleObjectCache");
      Directory.CreateDirectory(path);
    }

    public IEnumerable<string> GetAllObjects()
    {
      throw new NotImplementedException();
    }

    public string GetObject(string hash)
    {
      throw new NotImplementedException();
    }

    public IEnumerable<string> GetObjects(IEnumerable<string> id)
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
}
