using Autofac;
using Autofac.Core;
using Speckle.InterfaceGenerator;

namespace Speckle.Autofac.DependencyInjection;

[GenerateAutoInterface]
public class UnitOfWorkFactory : IUnitOfWorkFactory
{
  private readonly ILifetimeScope _parentScope;

  public UnitOfWorkFactory(ILifetimeScope parentScope)
  {
    _parentScope = parentScope;
  }

  public IUnitOfWork<TService> Resolve<TService>()
    where TService : class
  {
    ILifetimeScope? childScope = null;

    try
    {
      childScope = _parentScope.BeginLifetimeScope();
      var service = childScope.Resolve<TService>();

      return new UnitOfWork<TService>(childScope, service);
    }
    catch (DependencyResolutionException dre)
    {
      childScope?.Dispose();

      // POC: check exception and how to pass this further up
      throw new DependencyResolutionException(
        $"Dependency error resolving {typeof(TService)} within UnitOfWorkFactory",
        dre
      );
    }
  }
}
