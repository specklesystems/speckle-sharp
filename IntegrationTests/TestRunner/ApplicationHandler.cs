
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Sender;

interface IHandler
{
  public Task Setup();
  public Task<IEnumerable<ISender>> CreateSenders();

  public Task Teardown();
}

class RevitHandler: IHandler
{
  private string _workingFolder;
  private ServerSettings _serverSettings;
  private IEnumerable<(string, string)> _versions = new List<(string, string)>();
  private IList<ISender> _senders = new List<ISender>();

  public RevitHandler(string workingFolder, ServerSettings serverSettings)
  {
    _workingFolder = workingFolder;
    _serverSettings = serverSettings;
  }

  public async Task Setup()
  {
    await Teardown();

    var executables = _findInstalledRevitExecutables();
    _versions = executables.Select(exePath => (_getRevitVersion(exePath), exePath)).ToList();
    foreach (var (versionString, executablePath) in _versions)
    {
      Console.WriteLine($"Preparing {versionString} for test run @{executablePath}");
      await _prepareTestApplication(versionString);
    }
  }

  public async Task<IEnumerable<ISender>> CreateSenders(){
    foreach (var (versionString, executablePath) in _versions)
    {
      var workingFolder = _prepareWorkingFolder(versionString);
      var sourceFiles = _prepareSourceFiles(versionString, workingFolder);
      foreach (var sourceFile in sourceFiles)
      {
        // create new branch for RevitVersion/FileName
        string? targetBranch = await _prepareBranch(versionString, sourceFile);
         _senders.Add(new RevitSender(
           executablePath,
           versionString,
           workingFolder,
           sourceFile,
           _serverSettings.StreamId,
           targetBranch,
           _serverSettings.AccountId
         ));
      } 
    }
    return _senders;
  }

  public async Task Teardown()
  {
    Directory.Delete(_workingFolder, recursive:true);
    foreach (var (versionString, _) in _versions)
    {
      Directory.Delete(_revitPluginFolder(versionString), recursive: true);
    }
    await Task.Delay(1);
  }

  private static IEnumerable<string> _findInstalledRevitExecutables()
  {
    var matcher = new Matcher();
    matcher.AddInclude("**/Revit*/**/Revit.exe");

    string searchDirectory = "C:/Program Files/Autodesk";

    return matcher.GetResultsInFullPath(searchDirectory);
  }

  private async Task<string> _prepareBranch(string versionString, string sourceFile)
  {
    var fileName =Path.GetFileNameWithoutExtension(sourceFile); 
    return await _serverSettings.Client.BranchCreate(
      new Speckle.Core.Api.BranchCreateInput()
      {
        streamId = _serverSettings.StreamId,
        name = fileName,
        description = $"Integration test data created from ${versionString} ${fileName}."
      }
    );
  }

  private static string _getRevitVersion(string executablePath) => new Regex(@"(Revit 20[0-9][0-9])").Match(executablePath).Value;
  
  private string _prepareWorkingFolder(string revitVersion) => Directory.CreateDirectory(Path.Combine(_workingFolder, revitVersion)).FullName;
  
  private static IEnumerable<string> _prepareSourceFiles(string revitVersion, string workingFolder)
  {
    var testFiles = new []{
      "C:/spockle/ErrorBrepTest.rvt",
      // "C:/spockle/PROIECT CASA DE MODA.rvt",
      // "C:/spockle/PROIECT LIBRARIE BIBLIOTECA.rvt",
    };
    foreach (var testFile in testFiles)
    {
      var fileName = Path.GetFileName(testFile);
      var preparedFile =  Path.Combine(workingFolder, fileName);
      File.Copy(testFile, preparedFile, overwrite: true);
      yield return preparedFile;
    }
  }
  private static async Task _prepareTestApplication(string revitVersion)
  {
    //simulate that we would be getting this from a remote location
    await Task.Delay(10);
    var sourceFolder = Path.Combine("C:/spockle/plugin", revitVersion);
    var pluginFolder = _revitPluginFolder(revitVersion);
    Directory.CreateDirectory(pluginFolder);
    foreach (var file in Directory.GetFiles(sourceFolder))
    {
      File.Copy(file, Path.Combine(pluginFolder, Path.GetFileName(file)), overwrite: true);
    }
    //this method would be responsible for installing the tester plugin from ie object storage
    return;
  }

  private static string _revitPluginFolder(string revitVersion)
  {
    var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var revitVersionNumber = revitVersion.Replace("Revit ", "");
    var pluginFolder = Path.Combine(appdataFolder, "Autodesk/Revit/Addins", revitVersionNumber, "IntegrationTester");
    return pluginFolder;
  }
}