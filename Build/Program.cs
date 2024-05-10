using System;
using System.Collections.Generic;
using System.IO;
using GlobExpressions;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string CLEAN = "clean";
const string RESTORE = "restore";
const string BUILD = "build";
const string TEST = "test";
const string FORMAT = "format";
const string PUBLISH = "publish";

Target(
    CLEAN,
    ForEach("**/bin", "**/obj"),
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
Target(RESTORE, DependsOn(FORMAT), () => Run("dotnet", "restore"));

Target(
    BUILD,
    DependsOn(RESTORE),
    () =>
    {
        Run("dotnet", "build src/SharpCompress/SharpCompress.csproj -c Release --no-restore");
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

Target("default", DependsOn(PUBLISH), () => Console.WriteLine("Done!"));

await RunTargetsAndExitAsync(args);
