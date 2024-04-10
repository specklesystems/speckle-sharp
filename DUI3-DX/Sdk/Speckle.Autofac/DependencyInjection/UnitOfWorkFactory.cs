using Autofac;
using Autofac.Core;
using Speckle.Core.Logging;

namespace Speckle.Autofac.DependencyInjection;

public class UnitOfWorkFactory<TService> : IUnitOfWorkFactory<TService>
  where TService : class
{
  private readonly ILifetimeScope _parentScope;

  public UnitOfWorkFactory(ILifetimeScope parentScope)
  {
    _parentScope = parentScope;
  }

  public IUnitOfWork<TService> Resolve()
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
      throw new SpeckleException($"Dependency error resolving {typeof(TService)} within  UnitOfWorkFactory", dre);
    }
  }
}
