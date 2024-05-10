using Speckle.Autofac.DependencyInjection;

namespace Speckle.Autofac;

public static class ContainerRegistration
{
  public static void AddAutofac(this SpeckleContainerBuilder builder)
  {
    // send operation and dependencies
    builder.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
  }
}
