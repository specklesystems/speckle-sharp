using System.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Sender
{

  interface ISender
  {
    Task SendTo(string streamId);
  }


  class RevitSender : ISender
  {
    private string _applicationPath;

    public RevitSender(string applicationPath)
    {
      _applicationPath = applicationPath;
    }

    public async Task SendTo(string streamId)
    {
      await _prepareConfig();
      var process = Process.Start(_applicationPath);
      await process.WaitForExitAsync();
    }

    private static async Task _prepareConfig()
    {
      var tempPath = Path.Join(Path.GetTempPath(), "revit.json");
      var config = new Dictionary<string, object>(){
        {"sourceFile", "null"},
      };
      await File.WriteAllTextAsync(tempPath, System.Text.Json.JsonSerializer.Serialize(config));
    }
  }

  class Senders
  {
    private IList<ISender> _items;
    private ServerSettings _settings;

    public Senders(ServerSettings settings, IList<ISender> items)
    {
      _settings = settings;
      _items = items;
    }

    public async Task Send()
    {
      foreach (var sender in _items)
      {
        // 1. create a new target branch

        // 2. store the stream for later reuse

        // 3. send with the sender to the stream
        await sender.SendTo(_settings.StreamId);
      }
    }

    public static Senders Initialize(ServerSettings settings)
    {
      // Func<ServerSettings, IEnumerable<ISender>> senderGenerators = new []{

      // };
      var revitSenders = _initializeRevitSenders().ToList();
      //1. list of supported applications, with a list of supported versions, and a list of filetypes
      return new Senders(settings, new[]{
        new RevitSender(@"/home/gergojedlicska/Speckle/speckle-sharp/IntegrationTests/ConsoleSender/bin/Debug/net6.0/ConsoleSender")
      });
    }

    // private static IEnumerable<ISender> _initializeSenders()
    // {

    // }
    private static IEnumerable<ISender> _initializeRevitSenders() {
      var matcher = new Matcher();
      matcher.AddInclude("**/Revit*/**/Revit.exe");

      string searchDirectory = "C:/";

      var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(searchDirectory)));

      yield break;
    }
  }

}