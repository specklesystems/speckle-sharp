using System.Runtime.InteropServices;
using NUnit.Framework;
using Speckle.Core.Helpers;

namespace Speckle.Core.Tests.Unit.Helpers;

[TestFixture]
[TestOf(nameof(SpecklePathProvider))]
public class SpecklePathTests
{
  [Test]
  [TestCase("https://app.speckle.systems", "https://app.speckle.systems/api/ping")]
  [TestCase("https://app.speckle.systems/", "https://app.speckle.systems/api/ping")]
  [TestCase("https://app.speckle.systems/auth/login", "https://app.speckle.systems/api/ping")]
  [TestCase("https://latest.speckle.systems", "https://latest.speckle.systems/api/ping")]
  public void TestGetPingUrl(string given, string expected)
  {
    var x = Http.GetPingUrl(new Uri(given));
    Assert.That(x, Is.EqualTo(new Uri(expected)));
  }

  [Test]
  [TestCase("https://app.speckle.systems", "https://app.speckle.systems/favicon.ico")]
  [TestCase("https://app.speckle.systems/", "https://app.speckle.systems/favicon.ico")]
  [TestCase("https://app.speckle.systems/auth/login", "https://app.speckle.systems/favicon.ico")]
  [TestCase("https://latest.speckle.systems", "https://latest.speckle.systems/favicon.ico")]
  public void TestGetFaviconUrl(string given, string expected)
  {
    var x = Http.GetFaviconUrl(new Uri(given));
    Assert.That(x, Is.EqualTo(new Uri(expected)));
  }

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
