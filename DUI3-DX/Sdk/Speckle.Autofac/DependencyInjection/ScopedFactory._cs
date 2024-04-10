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
    // POC: do I need a new lifetime each time I do the resolve.
    // move this to ResolveScopedInstance() and rename this to UnitOfWork()
    // look at the disposal lifecycle, what we might need to return is a UoW object
    // we need a document to explain how this works and how to pick where to do it
    _lifetimeScope = parentScope.BeginLifetimeScope();
  }

  public T ResolveScopedInstance()
  {
    // POC: do I need a new lifetime each time I do the resolve
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
