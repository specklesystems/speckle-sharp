using Autofac.Features.Indexed;
using Speckle.InterfaceGenerator;

namespace Speckle.Autofac.DependencyInjection;

[GenerateAutoInterface]
public class Factory<TKey, TValue> : IFactory<TKey, TValue>
  where TValue : class
{
  private readonly IIndex<TKey, TValue> _types;

  public Factory(IIndex<TKey, TValue> types)
  {
    _types = types;
  }

  public TValue? ResolveInstance(TKey strongName)
  {
    if (_types.TryGetValue(strongName, out TValue value))
    {
      return value;
    }
    return null;
  }
}
