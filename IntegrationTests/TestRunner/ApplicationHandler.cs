
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Sender;

interface IHandler
{
  public Task Setup();
  public Task<IEnumerable<ISender>> CreateSenders();

  public void Teardown();
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
    Teardown();

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

  public void Teardown()
  {
    try
    {
      Directory.Delete(_workingFolder, recursive:true);
    }
    catch (Exception e)
    {
      //Ignore this if it doesnt exist.
    }
    
    foreach (var (versionString, _) in _versions)
    {
      var revitPluginFolder = _revitPluginFolder(versionString);
      Directory.Delete(revitPluginFolder, recursive: true);
      try
      {
        File.Delete(Path.Combine(Directory.GetParent(revitPluginFolder).FullName, "RevitAutoTest2022.addin"));
      }
      catch (Exception e)
      {
        // Ignore
      }
    }
  }

  private static IEnumerable<string> _findInstalledRevitExecutables()
  {
    var supportedVersions = new []{
      "2022"
    };
    var matcher = new Matcher();
    matcher.AddInclude("**/Revit*/**/Revit.exe");

    string searchDirectory = "C:/Program Files/Autodesk";

    return matcher
    .GetResultsInFullPath(searchDirectory)
    .Where(p => supportedVersions.Any(v => p.Contains(v)));
  }

  private async Task<string> _prepareBranch(string versionString, string sourceFile)
  {
    var fileName =Path.GetFileNameWithoutExtension(sourceFile);
    var branchName =$"{versionString}/{fileName}";
    var branchResult = await _serverSettings.Client.BranchCreate(
      new Speckle.Core.Api.BranchCreateInput()
      {
        streamId = _serverSettings.StreamId,
        name = branchName,
        description = $"Integration test data created from ${versionString} ${fileName}."
      }
    );
    return branchName;
  }

  private static string _getRevitVersion(string executablePath) => new Regex(@"(Revit 20[0-9][0-9])").Match(executablePath).Value;
  
  private string _prepareWorkingFolder(string revitVersion) => Directory.CreateDirectory(Path.Combine(_workingFolder, revitVersion)).FullName;
  
  private static IEnumerable<string> _prepareSourceFiles(string revitVersion, string workingFolder)
  {
    var testFiles = new []{
      "C:/spockle/ErrorBrepTest.rvt",
      "C:/spockle/PROIECT CASA DE MODA.rvt",
      //"C:/spockle/PROIECT LIBRARIE BIBLIOTECA.rvt",
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
    var addinsFolder = Directory.GetParent(pluginFolder).FullName;
    //this method would be responsible for installing the tester plugin from ie object storage
    Directory.CreateDirectory(pluginFolder);
    CopyFilesRecursively(sourceFolder,pluginFolder);
    var addinFile = "RevitAutoTest2022.addin"; 
    File.Move(Path.Combine(pluginFolder,addinFile), Path.Combine(addinsFolder,addinFile));
  }
  private static void CopyFilesRecursively(string sourcePath, string targetPath)
  {
    //Now Create all of the directories
    foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
    {
      Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
    }

    //Copy all the files & Replaces any files with the same name
    foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
    {
      File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }
  }
  private static string _revitPluginFolder(string revitVersion)
  {
    var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var revitVersionNumber = revitVersion.Replace("Revit ", "");
    var pluginFolder = Path.Combine(appdataFolder, "Autodesk/Revit/Addins", revitVersionNumber, "IntegrationTester");
    return pluginFolder;
  }
}