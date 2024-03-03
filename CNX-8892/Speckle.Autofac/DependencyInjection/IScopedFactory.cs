using System;

namespace Speckle.Autofac.DependencyInjection;

public interface IScopedFactory<T> : IDisposable
  where T : class
{
  T ResolveScopedInstance();
}
