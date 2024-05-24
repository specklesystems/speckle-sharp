using Speckle.Autofac.DependencyInjection;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Revit2023.Api;

public static class ContainerRegistration
{
  public static void AddRevit2023(this SpeckleContainerBuilder speckleContainerBuilder)
  {
    speckleContainerBuilder.AddTransient<IRevitUnitUtils, RevitUnitUtils>();
  }
}
