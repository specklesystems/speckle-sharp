using Autofac.Features.Indexed;

namespace Speckle.Autofac.DependencyInjection;

// POC: NEXT UP
// * begin scope: https://stackoverflow.com/questions/49595198/autofac-resolving-through-factory-methods
// Interceptors?

public class Factory<T, K> : IFactory<T, K>
  where T : class
{
  private readonly IIndex<K, T> _types;

  public Factory(IIndex<K, T> types)
  {
    _types = types;
  }

  public T ResolveInstance(K strongName)
  {
    return _types[strongName];
  }
}
