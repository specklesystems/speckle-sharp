using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.UI;
using Speckle.Connectors.DependencyInjection;

namespace Speckle.Connectors.RevitShared.RevitPlugin;

internal class RevitExternalApplication : IExternalApplication
{
  private IRevitPlugin? _revitPlugin = null;
  private SpeckleDiContainer? _container = null;
  private RevitSettings _revitSettings = new RevitSettings { RevitVersionName = "REVIT2023" };

  public Result OnStartup(UIControlledApplication application)
  {
    try
    {
      _container = new SpeckleDiContainer();

      // init DI
      _container
        .AddDependencies(new string[] { "<paths>" })
        .AddInstance<RevitSettings>(_revitSettings) // apply revit settings into DI
        .AddInstance<UIControlledApplication>(application) // inject UIControlledApplication application
        .Build();

      // resolve root object
      _revitPlugin = _container.Resolve<IRevitPlugin>();
      _revitPlugin.Initialise();
    }
    catch (Exception ex)
    {
      return Result.Failed;
    }

    return Result.Succeeded;
  }

  public Result OnShutdown(UIControlledApplication application)
  {
    try
    {
      _revitPlugin.Shutdown();
    }
    catch (Exception ex)
    {
      return Result.Failed;
    }

    return Result.Succeeded;
  }
}
