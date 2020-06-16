using System;
using System.Collections.Generic;
using System.IO;

namespace Speckle.Transports
{
  /// <summary>
  /// A transport that writes to disk, under a specified file path.
  /// </summary>
  public class DiskTransport : ITransport
  {

    public string TransportName { get; set; } = "Disk";

    /// <summary>
    /// The path were files will be saved.
    /// </summary>
    public string RootPath { get; set; }

    /// <summary>
    /// Flags wether to split the path or not.
    /// </summary>
    public bool SplitPath { get; set; }

    /// <summary>
    /// Creates a transport that writes & reads from disk, in the specified file path.
    /// </summary>
    /// <param name="basePath">The current environment's ApplicationData location.</param>
    /// <param name="applicationName">Defaults to "Speckle".</param>
    /// <param name="scope">Defaults to "Objects".</param>
    /// <param name="splitPath">Flags wether to split the object's location by first chars. E.g., an object with an id of "ABCDEF" will be stored in "AB/CD/EF", with "EF" being the actual file.</param>
    public DiskTransport(string basePath = null, string applicationName = "Speckle", string scope = "Objects", bool splitPath = true)
    {
      if (basePath == null)
        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

      RootPath = Path.Combine(basePath, applicationName, scope);

      Directory.CreateDirectory(RootPath);

      this.SplitPath = splitPath;
    }

    public string GetObject(string objectId)
    {
      var (_, filePath) = DirFileFromObjectId(objectId);
      if (File.Exists(filePath))
        return File.ReadAllText(filePath);

      throw new Exception($"Could not find the specified object ({filePath}).");
    }

    public Stream GetObjectStream(string objectId)
    {
      var (_, filePath) = DirFileFromObjectId(objectId);
      if (File.Exists(filePath))
        return File.OpenRead(filePath);

      throw new Exception($"Could not find the specified object ({filePath}).");
    }

    public IEnumerable<string> GetAllObjects()
    {
      if (SplitPath)
        throw new NotImplementedException("GetAllObjects not supported yet in disk transports with path splits.");

      var files = Directory.GetFiles(RootPath);
      foreach(var file in files)
      {
        yield return File.ReadAllText(file);
      }
    }

    public void SaveObject(string objectId, string serializedObject, bool overwrite = false)
    {
      var (dirPath, filePath) = DirFileFromObjectId(objectId);

      if (File.Exists(filePath) && !overwrite) return;
      if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

      File.WriteAllText(filePath, serializedObject);
    }

    /// <summary>
    /// Removes an object. <b>Do not use this method unless you know what you're doing; it can invalidate state!</b>
    /// </summary>
    /// <param name="objectId"></param>
    public void RemoveObject(string objectId)
    {
      var (_, filePath) = DirFileFromObjectId(objectId);
      File.Delete(filePath);
    }

    /// <summary>
    /// Internal method used to split hashes into file paths. Returns a tuple containing the path to the subfolder and the full file path.
    /// </summary>
    /// <param name="objectId"></param>
    /// <returns>A tuple containing the path to the subfolder and the full file path.</returns>
    public (string, string) DirFileFromObjectId(string objectId)
    {
      if (SplitPath == false)
      {
        return (RootPath, Path.Combine(RootPath, objectId));
      }

      var subFolder = objectId.Substring(0, 2);
      var secondSubFolder = objectId.Substring(2, 2);
      var filename = objectId.Substring(4);

      return (Path.Combine(RootPath, subFolder, secondSubFolder), Path.Combine(RootPath, subFolder, secondSubFolder, filename));
    }
  }
}
