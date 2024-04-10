using System;
using Autodesk.Revit.UI;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.Files;
using System.Reflection;
using System.IO;
using Autofac;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Revit.Plugin;

internal class RevitExternalApplication : IExternalApplication
{
  private IRevitPlugin? _revitPlugin;

  // POC: temp change to test
  public static AutofacContainer? _container;

  // POC: this is getting hard coded - need a way of injecting it
  //      I am beginning to think the shared project is not the way
  //      and an assembly which is invoked with some specialisation is the right way to go
  //      maybe subclassing, or some hook to inject som configuration
  private readonly RevitSettings _revitSettings;

  // POC: move to somewhere central?
  public static readonly DockablePaneId DoackablePanelId = new(new Guid("{f7b5da7c-366c-4b13-8455-b56f433f461e}"));

  public RevitExternalApplication()
  {
    // POC: load from JSON file?
    _revitSettings = new RevitSettings
    {
      RevitPanelName = "Speckle DUI3 (DI)",
      RevitTabName = "Speckle",
      RevitTabTitle = "Speckle DUI3 (DI)",
      RevitVersionName = "2023",
      RevitButtonName = "Speckle DUI3 (DI)",
      RevitButtonText = "Revit Connector",
      ModuleFolders = new string[] { Path.GetDirectoryName(typeof(RevitExternalApplication).Assembly.Location) }
    };
  }

  public Result OnStartup(UIControlledApplication application)
  {
    try
    {
      // POC: not sure what this is doing...  could be messing up our Aliasing????
      AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
      _container = new AutofacContainer(new StorageInfo());
      _container.PreBuildEvent += _container_PreBuildEvent;

      // init DI
      _container
        .LoadAutofacModules(_revitSettings.ModuleFolders)
        .AddSingletonInstance<RevitSettings>(_revitSettings) // apply revit settings into DI
        .AddSingletonInstance<UIControlledApplication>(application) // inject UIControlledApplication application
        .Build();

      // resolve root object
      _revitPlugin = _container.Resolve<IRevitPlugin>();
      _revitPlugin.Initialise();
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // POC: feedback?
      return Result.Failed;
    }

    return Result.Succeeded;
  }

  private void _container_PreBuildEvent(object sender, ContainerBuilder containerBuilder)
  {
    // POC: refactor the conversions to be simper, this method could be the basis for this
    // tbe event can probably go
    // IRawConversions should be separately injectable (and not Require an IHostObject... or NameAndRank attribute)
    // Name and Rank can become ConversionRank or something and be optional (otherwise it is rank 0)
    containerBuilder.RegisterRawConversions().InjectNamedTypes<IHostObjectToSpeckleConversion>();
  }

  public Result OnShutdown(UIControlledApplication application)
  {
    try
    {
      // POC: could this be more a generic Connector Init() Shutdown()
      // possibly with injected pieces or with some abstract methods?
      // need to look for commonality
      _revitPlugin.Shutdown();
    }
    catch (Exception e) when (!e.IsFatal())
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
