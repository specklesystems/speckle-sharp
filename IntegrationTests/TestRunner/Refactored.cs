
using Speckle.Core.Api;
using Speckle.Core.Credentials;

[Flags]
public enum SpeckleTransportOperations 
{
    None = 0,
    Send = 1,
    Receive = 2
}

public interface PipelineContext {
  // whatever this is, that is useful to pass between operations, and be dumped in front of a human in case of a failure
}

public class TestServerInfo
{
    public Account Account { get; set; }
    public string TestStreamId { get; set; }

    public static async Task<TestServerInfo> CreateServerInfo(string serverUrl = "https://latest.speckle.dev", string authToken = "")
    {
        //TODO Need to write local acc json on to disk
        
        var latestAcc = AccountManager.GetAccounts().First(a => a.serverInfo.url == serverUrl && a.token == authToken);
        var client = new Client(latestAcc);

        var streamInput = new StreamCreateInput
        {
            name = $"Integration Test {DateTime.Now}",
            isPublic = false,
        };
        var testStreamId = await client.StreamCreate(streamInput);
        
        var info = new TestServerInfo()
        {
            Account = latestAcc,
            TestStreamId = testStreamId,
        };
        return info;
    }
}

[Serializable]
public class SourceFileDescriptor
{
    public string FilePath { get; }
    public string Application { get; set; }
}

public class AutotestConfig
{
    public TestServerInfo TestServerInfo { get; set; }
    public ISourceFileRegistry SourceFileRegistry { get; set; }

    public static async Task<AutotestConfig> CreateAutotestConfig(Config config)
    {
        //TODO create registry from config
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
    public Dictionary<string, IEnumerable<SourceFileDescriptor>> SourceFileDescriptors { get; }
}



public class TestExecutor
{
    public async Task Execute()
    {
        // test workflow
        // 1. get test configuration -> AutotestConfig: app support matrix
        // 2. initialize available apps based on config -> usable application handler registry
        // 3. build executable test pipelines from config and available apps
        // 4. execute test pipelines:
        //   a. send a context object through the test pipeline to trace the steps of the workflow
        //      ie: count the objects 
        throw new NotImplementedException();
    }
} 

