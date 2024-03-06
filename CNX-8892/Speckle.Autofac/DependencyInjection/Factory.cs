using Autofac.Features.Indexed;

namespace Speckle.Autofac.DependencyInjection;

public class Factory<K, T> : IFactory<K, T>
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
