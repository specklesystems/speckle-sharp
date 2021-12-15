using System.IO;
using System.Text.Json;

namespace RevitAutoTest2022
{
  class RevitConfig
  {
    public string SourceFile { get; set; }
    public string WorkingFolder { get; set; }
    public string TargetStream { get; set; }
    public string TargetBranch { get; set; }
    public string SenderId { get; set; }

    public static RevitConfig LoadConfig(string filePath)
    {
      var workingFolder = Path.GetDirectoryName(filePath);
      var fileName = Path.GetFileNameWithoutExtension(filePath);
      var configPath = Path.Combine(workingFolder, $"{fileName}.config.json");
      if (!File.Exists(configPath)) throw new FileNotFoundException(configPath);

      var revitConfig = JsonSerializer.Deserialize<RevitConfig>(File.ReadAllText(configPath));
      return revitConfig;
    }
  }

  class SendResult
  {
    public SendResult(bool success, string log)
    {
      Success = success;
      Log = log;
    }

    public bool Success { get; set; }
    public string Log { get; set; }

    public void Save(string filePath)
    {
      var workingFolder = Path.GetDirectoryName(filePath);
      var fileName = Path.GetFileNameWithoutExtension(filePath);
      var resultPath = Path.Combine(workingFolder, $"{fileName}.result.json");
      File.WriteAllText(resultPath, JsonSerializer.Serialize(this));
    }

  }
}
