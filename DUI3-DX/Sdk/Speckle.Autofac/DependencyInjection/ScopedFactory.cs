using System;
using Autofac;

namespace Speckle.Autofac.DependencyInjection;

public class ScopedFactory<T> : IScopedFactory<T>
  where T : class
{
  private readonly ILifetimeScope _lifetimeScope;
  private bool _disposed = false;

  public ScopedFactory(ILifetimeScope parentScope)
  {
    _lifetimeScope = parentScope.BeginLifetimeScope();
  }

  public T ResolveScopedInstance()
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
    if (disposing && !_disposed)
    {
      // POC: check: but I think dispose will end the scope
      _lifetimeScope.Dispose();
      _disposed = true;
    }
  }

  // might be needed in future...
  ~ScopedFactory()
  {
    Dispose(false);
  }
}
