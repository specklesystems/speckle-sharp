using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Speckle.Core.Helpers;

namespace Speckle.Core.Tests.Unit.Helpers;

[TestFixture]
[TestOf(nameof(SpecklePathProvider))]
public class SpecklePathTests
{
  [Test]
  public void TestUserApplicationDataPath()
  {
    var userPath = SpecklePathProvider.UserApplicationDataPath();
    string pattern;

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      pattern = @"C:\\Users\\.*\\AppData\\Roaming";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      pattern = @"\/Users\/.*\/\.config";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      // if running under root user, the .config folder is in another location...
      if (userPath.StartsWith("/root"))
      {
        pattern = @"\/root/\.config";
      }
      else
      {
        pattern = @"\/home/.*/\.config";
      }
    }
    else
    {
      throw new NotImplementedException("Your OS platform is not supported");
    }

    Assert.That(userPath, Does.Match(pattern));
  }

  [Test]
  public void TestUserApplicationDataPathOverride()
  {
    var newPath = Path.GetTempPath();
    SpecklePathProvider.OverrideApplicationDataPath(newPath);
    Assert.That(SpecklePathProvider.UserApplicationDataPath(), Is.EqualTo(newPath));
    SpecklePathProvider.OverrideApplicationDataPath(null);
  }

  [Test]
  public void TestInstallApplicationDataPath()
  {
    var installPath = SpecklePathProvider.InstallApplicationDataPath;
    string pattern;

    if (string.IsNullOrEmpty(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
    {
      pattern = @"\/root";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      // this will prob fail on windows
      pattern = @"C:\\Users\\.*\\AppData\\Roaming";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      pattern = @"\/Users\/.*\/\.config";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      // if running under root user, the .config folder is in another location...
      if (installPath.StartsWith("/root"))
      {
        pattern = @"\/root/\.config";
      }
      else
      {
        pattern = @"\/home/.*/\.config";
      }
    }
    else
    {
      throw new NotImplementedException("Your OS platform is not supported");
    }

    Assert.That(installPath, Does.Match(pattern));
  }
}
