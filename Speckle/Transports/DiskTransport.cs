using System;
using System.IO;

namespace Speckle.Transports
{
  /// <summary>
  /// A transport that writes to disk, under a specified file path.
  /// </summary>
  public class DiskTransport : ITransport
  {
    public string RootPath { get; set; }

    /// <summary>
    /// Creates a transport that writes to disk, in the specified file path. Files are saved in folders created from the first two chars of the hash.
    /// </summary>
    /// <param name="path">If left null, defaults to a "SpeckleObjectCache" folder in the current environment's ApplicationData location.</param>
    public DiskTransport(string path = null)
    {
      if (path == null)
        RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpeckleObjectCache");

      Directory.CreateDirectory(RootPath);
    }

    public string GetObject(string hash)
    {
      var (dirPath, filePath) = DirFileFromHash(hash);
      if (File.Exists(filePath))
        return File.ReadAllText(filePath);

      throw new Exception($"Could not find the specified object ({filePath}).");
    }

    public void SaveObject(string hash, string serializedObject)
    {
      var (dirPath, filePath) = DirFileFromHash(hash);

      if (File.Exists(filePath)) return;
      if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

      File.WriteAllTextAsync(filePath, serializedObject);
    }

    /// <summary>
    /// Internal method used to split hashes into file paths. Returns a tuple containing the path to the subfolder and the full file path.
    /// </summary>
    /// <param name="hash"></param>
    /// <returns>A tuple containing the path to the subfolder and the full file path.</returns>
    (string, string) DirFileFromHash(string hash)
    {
      var subFolder = hash.Substring(0, 2);
      var filename = hash.Substring(2);

      return (Path.Combine(RootPath, subFolder), Path.Combine(RootPath, subFolder, filename));
    }
  }
}
