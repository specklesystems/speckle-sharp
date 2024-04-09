using System.Text.Json;

namespace Speckle.DllConflictManagement;

public sealed class DllConflictManagmentOptionsLoader
{
  private readonly string _filePath;

  public DllConflictManagmentOptionsLoader(string filePath)
  {
    _filePath = filePath;
  }

  private readonly string _fileName = "DllConflictManagmentOptions.json";
  private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

  private string FullPath => Path.Combine(_filePath, _fileName);

  public DllConflictManagmentOptions LoadOptions()
  {
    if (!File.Exists(FullPath))
    {
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
}
