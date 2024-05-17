using Speckle.InterfaceGenerator;

namespace Speckle.Autofac.Files;

[GenerateAutoInterface]
public class StorageInfo : IStorageInfo
{
  public IEnumerable<string> GetFilenamesInDirectory(string path, string pattern)
  {
    return Directory.GetFiles(path, pattern);
  }
}
