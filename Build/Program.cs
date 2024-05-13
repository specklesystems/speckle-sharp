using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GlobExpressions;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string CLEAN = "clean";
const string RESTORE = "restore";
const string BUILD = "build";
const string TEST = "test";
const string FORMAT = "format";
const string ZIP = "zip";
const string TRIGGER_WORKFLOW = "trigger-workflow";

var arguments = new List<string>();
if (args.Length > 1)
{
  arguments = args.ToList();
  args = new[] { arguments.First() };
  arguments = arguments.Skip(1).ToList();
}

Target(
  CLEAN,
  ForEach("**/output"),
  dir =>
  {
    IEnumerable<string> GetDirectories(string d)
    {
      return Glob.Directories(".", d);
    }

    void RemoveDirectory(string d)
    {
      if (Directory.Exists(d))
      {
        Console.WriteLine(d);
        Directory.Delete(d, true);
      }
    }

    foreach (var d in GetDirectories(dir))
    {
      RemoveDirectory(d);
    }
  }
);

Target(
  FORMAT,
  () =>
  {
    Run("dotnet", "tool restore");
    Run("dotnet", "csharpier --check .");
  }
);
Target(
  RESTORE,
  DependsOn(FORMAT),
  () =>
  {
    var path = arguments.First();
    Run("dotnet", $"dotnet restore --locked-mode {path}");
  }
);

Target(
  BUILD,
  () =>
  {
    var path = arguments.First();
    Run("msbuild", $"{path} /p:Configuration=Release /p:IsDesktopBuild=false /p:NuGetRestorePackages=false -v:m");
  }
);

Target(
  TEST,
  DependsOn(BUILD),
  () =>
  {
    IEnumerable<string> GetFiles(string d)
    {
      return Glob.Files(".", d);
    }

    foreach (var file in GetFiles("**/*.Test.csproj"))
    {
      Run("dotnet", $"test {file} -c Release --no-restore --verbosity=normal");
    }
  }
);

Target(
  ZIP,
  () =>
  {
    var path = arguments.First();
    var framework = arguments.Skip(1).First();
    var fullPath = Path.Combine(".", path, "bin", "Release", framework);
    var outputDir = Path.Combine(".", "output");
    Directory.CreateDirectory(outputDir);
    var outputPath = Path.Combine(outputDir, $"{new DirectoryInfo(path).Name}.zip");
    Console.WriteLine($"Zipping: '{fullPath}' to '{outputPath}'");
    ZipFile.CreateFromDirectory(fullPath, outputPath);
  }
);

Target(
  TRIGGER_WORKFLOW,
  async () =>
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }
);

Target("default", DependsOn(ZIP), () => Console.WriteLine("Done!"));

await RunTargetsAndExitAsync(args).ConfigureAwait(true);
