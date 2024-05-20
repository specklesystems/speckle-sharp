using Autofac;

namespace Speckle.Autofac.DependencyInjection;

public class SpeckleContainer
{
  private readonly IContainer _container;

  public SpeckleContainer(IContainer container)
  {
    _container = container;
  }

  public T Resolve<T>()
    where T : class
  {
    return _container.Resolve<T>();
  }
}
