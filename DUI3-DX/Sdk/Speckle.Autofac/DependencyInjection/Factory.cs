using Autofac.Features.Indexed;
using Speckle.InterfaceGenerator;

namespace Speckle.Autofac.DependencyInjection;

[GenerateAutoInterface]
public class Factory<TValue> : IFactory<TValue>
  where TValue : class
{
  private readonly IIndex<string, TValue> _types;

  public Factory(IIndex<string, TValue> types)
  {
    _types = types;
  }

  public TValue? ResolveInstance(string strongName)
  {
    if (_types.TryGetValue(strongName, out TValue value))
    {
      return value;
    }
    return null;
  }
}
