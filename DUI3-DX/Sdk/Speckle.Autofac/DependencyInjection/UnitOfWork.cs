using Autofac;
using InterfaceGenerator;

namespace Speckle.Autofac.DependencyInjection;

public partial interface IUnitOfWork<TService> : IDisposable
  where TService : class { }

[GenerateAutoInterface]
public sealed class UnitOfWork<TService> : IUnitOfWork<TService>
  where TService : class
{
  private readonly ILifetimeScope _unitOfWorkScope;
  private bool _notDisposed = true;

  public UnitOfWork(ILifetimeScope unitOfWorkScope, TService service)
  {
    _unitOfWorkScope = unitOfWorkScope;
    Service = service;
  }

  public TService Service { get; private set; }

  [AutoInterfaceIgnore]
  public void Dispose() => Disposing(true);

  private void Disposing(bool fromDispose)
  {
    if (_notDisposed && fromDispose)
    {
      _unitOfWorkScope.Dispose();
      _notDisposed = false;
    }
  }
}
