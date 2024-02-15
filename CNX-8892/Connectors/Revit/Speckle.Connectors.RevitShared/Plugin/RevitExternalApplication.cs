using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.UI;
using Speckle.Autofac.DependencyInjection;
using Speckle.Autofac.Files;

namespace Speckle.Connectors.Revit.Plugin;

internal class RevitExternalApplication : IExternalApplication
{
  private IRevitPlugin? _revitPlugin = null;
  private AutofacContainer? _container = null;

  // POC: this is getting hard coded - need a way of injecting it
  //      I am beginning to think the shared project is not the way
  //      and an assembly which is invoked with some specialisation is the right way to go
  //      maybe subclassing, or some hook to inject som configuration
  private RevitSettings _revitSettings = new RevitSettings { RevitVersionName = "REVIT2023" };

  public Result OnStartup(UIControlledApplication application)
  {
    try
    {
      _container = new AutofacContainer(new StorageInfo());

      // *** AUTOFAC MODULES ***

      // init DI
      _container
        .LoadAutofacModules(new string[] { "<paths>" })
        .AddInstance<RevitSettings>(_revitSettings) // apply revit settings into DI
        .AddInstance<UIControlledApplication>(application) // inject UIControlledApplication application
        .Build();

      // resolve root object
      _revitPlugin = _container.Resolve<IRevitPlugin>();
      _revitPlugin.Initialise();
    }
    catch (Exception ex)
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
      _revitPlugin.Shutdown();
    }
    catch (Exception ex)
    {
      // POC: feedback?
      return Result.Failed;
    }

    return Result.Succeeded;
  }
}
