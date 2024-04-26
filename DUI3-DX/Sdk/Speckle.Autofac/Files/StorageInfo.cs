using System.Collections.Generic;
using System.IO;

namespace Speckle.Autofac.Files;

public class StorageInfo : IStorageInfo
{
  public IEnumerable<string> GetFilenamesInDirectory(string path, string pattern)
  {
    return Directory.GetFiles(path, pattern);
  }
}
