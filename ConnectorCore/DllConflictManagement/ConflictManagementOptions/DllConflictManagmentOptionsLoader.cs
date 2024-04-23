using Speckle.DllConflictManagement.Serialization;

namespace Speckle.DllConflictManagement.ConflictManagementOptions;

public sealed class DllConflictManagmentOptionsLoader
{
  private readonly ISerializer _serializer;
  private readonly string _filePath;
  private readonly string _fileName;

  public DllConflictManagmentOptionsLoader(ISerializer serializer, string hostAppName, string hostAppVersion)
  {
    _serializer = serializer;
    _filePath = Path.Combine(GetAppDataFolder(), "Speckle", "DllConflictManagement");
    _fileName = $"DllConflictManagmentOptions-{hostAppName}{hostAppVersion}.json";
  }

  private string FullPath => Path.Combine(_filePath, _fileName);

  public DllConflictManagmentOptions LoadOptions()
  {
    if (!File.Exists(FullPath))
    {
      Directory.CreateDirectory(_filePath);
      var defaultOptions = new DllConflictManagmentOptions(new HashSet<string>());
      SaveOptions(defaultOptions);
      return defaultOptions;
    }

    string jsonString = File.ReadAllText(FullPath);
    return _serializer.Deserialize<DllConflictManagmentOptions>(jsonString)!;
  }

  public void SaveOptions(DllConflictManagmentOptions options)
  {
    var json = _serializer.Serialize(options);
    File.WriteAllText(FullPath, json);
  }

  private string GetAppDataFolder()
  {
    return Environment.GetFolderPath(
      Environment.SpecialFolder.ApplicationData,
      // if the folder doesn't exist, we get back an empty string on OSX,
      // which in turn, breaks other stuff down the line.
      // passing in the Create option ensures that this directory exists,
      // which is not a given on all OS-es.
      Environment.SpecialFolderOption.Create
    );
  }
}
