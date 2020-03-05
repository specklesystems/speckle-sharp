using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Speckle.Transports
{
  public interface ITransport
  {

    //public Task<string> SaveObjectsAsync(IEnumerable<string> @objects);

    public void SaveObject(string hash, string serializedObject);

    //public void SaveObjects(Dictionary<string, string> @objects);

    //public Task GetObjectsAsync(IEnumerable<string> id);

    public string GetObject(string hash);

    //public IEnumerable<string> GetObjects(IEnumerable<string> hashes);

    //public IEnumerable<string> GetAllObjects();
  }
}
