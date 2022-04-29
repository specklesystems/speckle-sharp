using Speckle.Core.Kits;

[Flags]
public enum SpeckleTransportOperations
{
  None = 0,
  Send = 1,
  Receive = 2
}

public interface PipelineContext
{
  // whatever this is, that is useful to pass between operations, and be dumped in front of a human in case of a failure
}

[Serializable]
public class SourceFileDescriptor
{
  public SourceFileDescriptor(string filePath, string application)
  {
    FilePath = filePath;
    Application = application;
  }

  public string FilePath { get; }
  public string Application { get; set; }
}

public class AutotestConfigOld
{
  public TestContext TestContext { get; set; }
  public ISourceFileRegistry SourceFileRegistry { get; set; }

  public static async Task<AutotestConfig> CreateAutotestConfig(Config config)
  {
    var temp = new Config();
    temp.ValidSendReceivePairs = new List<(string sender, string receiver)>
        {
            (Applications.Rhino7, Applications.Grasshopper),
            (Applications.Revit2021, Applications.Rhino7)
        };

    temp.PipelineDescriptors = new List<List<string>>
        {
            new() { "Rhino", "Revit", "Grasshopper" },
            new() { "Revit", "Grasshopper", "Revit" },
        };
    temp.FileDescriptors = new List<SourceFileDescriptor>
        {
            new("fakepath.3dm", Applications.Rhino7),
            new("fakepath.rvt", Applications.Revit2022),
            new("fakepath.gh", Applications.Grasshopper)
        };

    var autoTestConfig = new AutotestConfig();





    return autoTestConfig;
  }
}

[Serializable]
public class Config
{
  public List<SourceFileDescriptor> FileDescriptors { get; set; }

  public List<(string sender, string receiver)> ValidSendReceivePairs { get; set; }

  public List<List<string>> PipelineDescriptors { get; set; }
}


public interface ISourceFileRegistry
{
  public Dictionary<string, List<SourceFileDescriptor>> SourceFileDescriptors { get; }
}

public class SourceFileRegistry : ISourceFileRegistry
{
  public Dictionary<string, List<SourceFileDescriptor>> SourceFileDescriptors { get; set; }

  public static Task<SourceFileRegistry> CreateFileRegistry(List<SourceFileDescriptor> fileDescriptors)
  {
    var registry = new SourceFileRegistry();
    var sourceFileDescriptors = new Dictionary<string, List<SourceFileDescriptor>>();

    fileDescriptors.ForEach(descriptor =>
    {
      // Validation may be needed on the descriptor instead.
      if (!File.Exists(descriptor.FilePath))
        return;


      if (!sourceFileDescriptors.ContainsKey(descriptor.Application))
        sourceFileDescriptors[descriptor.Application] = new List<SourceFileDescriptor>();
      sourceFileDescriptors[descriptor.Application].Add(descriptor);
    });
    registry.SourceFileDescriptors = sourceFileDescriptors;

    return Task.Run(() => registry);
  }
}

