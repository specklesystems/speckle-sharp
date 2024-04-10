using System.IO;
using System.Text.Json;

namespace Speckle.DllConflictManagement;

public sealed class DllConflictManagmentOptionsLoader
{
  private readonly string _filePath;
  private readonly string _fileName;

  public DllConflictManagmentOptionsLoader(string hostAppName, string hostAppVersion)
  {
    _filePath = Path.Combine(GetAppDataFolder(), "Speckle", "DllConflictManagement");
    _fileName = $"DllConflictManagmentOptions-{hostAppName}{hostAppVersion}.json";
  }

  private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

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
    return JsonSerializer.Deserialize<DllConflictManagmentOptions>(jsonString)!;
  }

  public void SaveOptions(DllConflictManagmentOptions options)
  {
    var json = JsonSerializer.Serialize(options, _jsonSerializerOptions);
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
