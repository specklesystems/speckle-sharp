using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Speckle.Connectors.Revit.Plugin.DllConflictManagment;

public sealed class DllConflictManagmentOptionsLoader
{
  private readonly string _filePath = Path.GetDirectoryName(typeof(DllConflictManager).Assembly.Location);

  private readonly string _fileName = "DllConflictManagmentOptions.json";

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
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(options, jsonOptions);
    File.WriteAllText(FullPath, json);
  }
}
