using Speckle.Autofac.DependencyInjection;
using Speckle.InterfaceGenerator;

namespace Speckle.Converters.Common;

[GenerateAutoInterface]
public sealed class ConverterResolver<TConverter> : IConverterResolver<TConverter>
  where TConverter : class
{
  private readonly IFactory<TConverter> _factory;

  public ConverterResolver(IFactory<TConverter> factory)
  {
    _factory = factory;
  }

  public TConverter? GetConversionForType(Type objectType)
  {
    if (objectType.BaseType == null)
    {
      //We've reached the top of the inheritance tree
      return null;
    }

    return _factory.ResolveInstance(objectType.Name) ?? GetConversionForType(objectType.BaseType);
  }
}
