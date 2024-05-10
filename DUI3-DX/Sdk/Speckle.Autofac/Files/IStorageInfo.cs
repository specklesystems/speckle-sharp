namespace Speckle.Autofac.Files;

public interface IStorageInfo
{
  IEnumerable<string> GetFilenamesInDirectory(string path, string pattern);
}
