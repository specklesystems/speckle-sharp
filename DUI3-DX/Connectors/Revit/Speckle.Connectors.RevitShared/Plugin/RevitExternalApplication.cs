using Autodesk.Revit.UI;
using Speckle.Autofac.DependencyInjection;
using System.Reflection;
using Speckle.Autofac;
using Speckle.Connectors.Utils;
using Speckle.Core.Logging;

namespace Speckle.Connectors.Revit.Plugin;

internal sealed class RevitExternalApplication : IExternalApplication
{
  private IRevitPlugin? _revitPlugin;

  private SpeckleContainer? _container;

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
    _revitSettings = new RevitSettings(
      "Speckle New UI",
      "Speckle",
      "Speckle New UI",
      "2023",
      "Speckle New UI",
      "Revit",
      new[] { Path.GetDirectoryName(typeof(RevitExternalApplication).Assembly.Location) },
      "Revit Connector",
      "2023" //POC: app version?
    );
  }

  public Result OnStartup(UIControlledApplication application)
  {
    try
    {
      // POC: not sure what this is doing...  could be messing up our Aliasing????
      AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.OnAssemblyResolve<RevitExternalApplication>;
      var containerBuilder = SpeckleContainerBuilder.CreateInstance();
      // init DI
      _container = containerBuilder
        .LoadAutofacModules(Assembly.GetExecutingAssembly(), _revitSettings.ModuleFolders.NotNull())
        .AddSingleton(_revitSettings) // apply revit settings into DI
        .AddSingleton(application) // inject UIControlledApplication application
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

  public Result OnShutdown(UIControlledApplication application)
  {
    try
    {
      // POC: could this be more a generic Connector Init() Shutdown()
      // possibly with injected pieces or with some abstract methods?
      // need to look for commonality
      _revitPlugin?.Shutdown();
    }
    catch (Exception e) when (!e.IsFatal())
    {
      // POC: feedback?
      return Result.Failed;
    }

    return Result.Succeeded;
  }
}
