using System;
using Autofac;

namespace Speckle.Autofac.DependencyInjection;

public class ScopedFactory<T> : IScopedFactory<T>, IDisposable
  where T : class
{
  private readonly ILifetimeScope _lifetimeScope;
  private bool _disposed = false;

  public ScopedFactory(ILifetimeScope lifetimeScope)
  {
    _lifetimeScope = lifetimeScope.BeginLifetimeScope();
  }

  public T CreateScopedInstance()
  {
    return _lifetimeScope.Resolve<T>();
  }

  public void Dispose()
  {
    Dispose(true);

    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      _lifetimeScope.Dispose();
      _disposed = true;
    }
  }
}
