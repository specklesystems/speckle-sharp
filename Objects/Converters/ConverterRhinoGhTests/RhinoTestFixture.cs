using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace ConverterRhinoGhTests
{
  /// <summary>
  /// Shared test context across unit tests that loads rhinocommon.dll and grasshopper.dll
  /// </summary>
  public class RhinoTestFixture : IDisposable
  {
    private bool initialized = false;
    private static string rhinoDir;
    private Rhino.Runtime.InProcess.RhinoCore _rhinoCore;

    /// <summary>
    /// Empty Constuctor
    /// </summary>
    public RhinoTestFixture()
    {
      //get the correct rhino 7 installation directory
      rhinoDir =
        Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path", null) as string
        ?? string.Empty;
      Assert.True(Directory.Exists(rhinoDir), String.Format("Rhino system dir not found: {0}", rhinoDir));

      // Make sure we are running the tests as 64x
      Assert.True(Environment.Is64BitProcess, "Tests must be run as x64");

      if (initialized)
      {
        throw new InvalidOperationException("Initialize Rhino.Inside once");
      }
      else
      {
        RhinoInside.Resolver.Initialize();
        initialized = true;
      }

      // Set path to rhino system directory
      string envPath = Environment.GetEnvironmentVariable("path");
      Environment.SetEnvironmentVariable("path", envPath + ";" + rhinoDir);

      // Start a headless rhino instance using Rhino.Inside
      StartRhino();

      // We have to load grasshopper.dll on the current AppDomain manually for some reason
      AppDomain.CurrentDomain.AssemblyResolve += ResolveGrasshopper;
    }

    /// <summary>
    /// Starting Rhino - loading the relevant libraries
    /// </summary>
    [STAThread]
    public void StartRhino()
    {
      _rhinoCore = new Rhino.Runtime.InProcess.RhinoCore(null, Rhino.Runtime.InProcess.WindowStyle.NoWindow);
    }

    /// <summary>
    /// Add Grasshopper.dll to the current Appdomain
    /// </summary>
    private Assembly ResolveGrasshopper(object sender, ResolveEventArgs args)
    {
      var name = args.Name;

      if (!name.StartsWith("Grasshopper"))
      {
        return null;
      }

      var path = Path.Combine(
        Path.GetFullPath(Path.Combine(rhinoDir, @"..\")),
        "Plug-ins\\Grasshopper\\Grasshopper.dll"
      );
      return Assembly.LoadFrom(path);
    }

    /// <summary>
    /// Disposing the context after running all the tests
    /// </summary>
    public void Dispose()
    {
      // do nothing or...
      _rhinoCore?.Dispose();
      _rhinoCore = null;
    }
  }

  /// <summary>
  /// Collection Fixture - shared context across test classes
  /// </summary>
  [CollectionDefinition("RhinoTestingCollection")]
  public class RhinoCollection : ICollectionFixture<RhinoTestFixture>
  {
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
  }
}
