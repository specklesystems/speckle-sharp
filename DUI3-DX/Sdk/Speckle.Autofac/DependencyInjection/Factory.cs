using Autofac.Features.Indexed;

namespace Speckle.Autofac.DependencyInjection;

public class Factory<TKey, TValue> : IFactory<TKey, TValue>
  where TValue : class
{
  private readonly IIndex<TKey, TValue> _types;

  public Factory(IIndex<TKey, TValue> types)
  {
    _types = types;
  }

  public TValue ResolveInstance(TKey strongName)
  {
    return _types[strongName];
  }
}
