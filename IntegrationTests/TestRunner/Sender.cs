using System.Diagnostics;
using System.Text.Json;

namespace Sender
{

  interface ISender
  {
    SendResult? SendResult {get;}
    Task<SendResult> Send();
  }

  record SendResult
  {
    public bool Success {get; init;}
    public string Log {get; init;}

    public SendResult(bool success, string log)
    {
      Success = success;
      Log = log;
    }
  }

  record RevitConfig
  {
    public string SourceFile {get; init; }
    public string WorkingFolder {get; init; }
    public string TargetStream {get; init; }
    public string TargetBranch {get; init; }
    public string SenderId {get; init; }

    public RevitConfig(string sourceFile, string workingFolder, string targetStream, string targetBranch, string senderId)
    {
      SourceFile = sourceFile;
      WorkingFolder = workingFolder;
      TargetStream = targetStream;
      TargetBranch = targetBranch;
      SenderId = senderId;
    }
  }

  class RevitSender : ISender
  {
    private string _applicationPath;
    private string _revitVersion;
    private RevitConfig _config;
    public SendResult? SendResult {get; private set;}

    public RevitSender(
      string applicationPath,
      string revitVersion,
      string workingFolder,
      string sourceFile,
      string targetStream,
      string targetBranch,
      string senderId
)
    {
      _applicationPath = applicationPath;
      _revitVersion = revitVersion;
      _config = new (sourceFile, workingFolder, targetStream, targetBranch, senderId);
    }

    public async Task<SendResult> Send()
    {
      await _prepareConfig();
      var process = Process.Start(_applicationPath);
      // var process = Process.Start(_applicationPath, _config.SourceFile);

      return await _waitForResult();
    }

    private async Task<SendResult> _waitForResult()
    {
      var resultFile = Path.Combine(
        _config.WorkingFolder,
        $"{Path.GetFileNameWithoutExtension(_config.SourceFile)}.result.json"
      );
      Console.WriteLine(resultFile);
      while (!File.Exists(resultFile))
      {
        await Task.Delay(10000);
      }
      var result = JsonSerializer.Deserialize<SendResult>(File.ReadAllText(resultFile)) ?? throw new Exception("Result deserialization failed");
      SendResult = result;
      Console.WriteLine("Sent.");
      return result;
    }

    private async Task _prepareConfig()
    {
      var tempPath = Path.Join(_config.WorkingFolder, $"{Path.GetFileNameWithoutExtension(_config.SourceFile)}.config.json");
      await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(_config));
    }
  }

  class TestExecutor: IHandler
  {
    private ServerSettings _settings;
    private IEnumerable<IHandler> _handlers = new List<IHandler>();

    public TestExecutor(ServerSettings settings)
    {
      _settings = settings;
    }

    public async Task<IEnumerable<SendResult>> Send()
    {
      var senders = (await Task.WhenAll(_handlers.Select(h => h.CreateSenders()))).SelectMany(x => x);
      return await Task.WhenAll(senders.Select(s => s.Send()));
    }

    public async Task Setup()
    {
      var workingFolder = Path.Join(Path.GetTempPath(), "SpeckleTestFolder");
      //1. list of supported applications, with a list of supported versions,
      // and a list of filetypes
      _handlers = new []{
        new RevitHandler(workingFolder, _settings)
      };
      await Task.WhenAll(_handlers.Select(h => h.Setup()));
    }

    public Task<IEnumerable<ISender>> CreateSenders()
    {
      throw new NotImplementedException();
    }

    public void Teardown() 
    {
      foreach (var handler in _handlers)
      {
        handler.Teardown();
      }
    }
  }
}