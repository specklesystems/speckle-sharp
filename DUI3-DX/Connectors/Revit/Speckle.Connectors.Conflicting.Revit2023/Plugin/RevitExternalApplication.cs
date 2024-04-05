using System;
using Autodesk.Revit.UI;
using System.Reflection;
using System.IO;
using Serilog.Events;
using System.Diagnostics;

namespace Speckle.Connectors.Conflicting.Revit2023.Plugin;

internal class RevitExternalApplication : IExternalApplication
{
  public RevitExternalApplication() { }

  public Result OnStartup(UIControlledApplication application)
  {
    try
    {
      // POC: not sure what this is doing...  could be messing up our Aliasing????
      AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

      var x = new Objects.BuiltElements.Revit.FamilyInstance();
      var y = new Speckle.Core.Serialisation.BaseObjectSerializer();

      var a = LevelAlias.Off;
      Trace.WriteLine(a);

      var plugin = new RevitPlugin(application);
      plugin.Initialise();
    }
    catch (Exception e)
    {
      return Result.Failed;
    }

    return Result.Succeeded;
  }

  public Result OnShutdown(UIControlledApplication application)
  {
    try { }
    catch (Exception e)
    {
      // POC: feedback?
      return Result.Failed;
    }

    return Result.Succeeded;
  }

  private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
  {
    // POC: tight binding to files
    Assembly assembly = null;
    string name = args.Name.Split(',')[0];
    string path = Path.GetDirectoryName(typeof(RevitPlugin).Assembly.Location);

    if (path != null)
    {
      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
      {
        assembly = Assembly.LoadFrom(assemblyFile);
      }
    }

    return assembly;
  }
}
