using YamlDotNet.Serialization.NamingConventions;

public struct AppVersion
{
  public string Version;
  public IEnumerable<string> SourceFiles;
}

public struct AppConfig
{
  public string Name;
  public IEnumerable<AppVersion> Versions;


}

public struct AutotestConfig
{
  public IEnumerable<AppConfig> Apps;
  public string ResultStorage;
  public IEnumerable<IEnumerable<string>> Pipelines;
}

public class ConfigLoader
{
  public static AutotestConfig Load()
  {
    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
    var myConfig = deserializer.Deserialize<AutotestConfig>(File.ReadAllText("./config.example.yaml"));
    return myConfig;
  }
}
