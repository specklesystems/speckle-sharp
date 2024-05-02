using System;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Common;

public sealed class RecursiveConverterResolver<TConverter> : IConverterResolver<TConverter>
  where TConverter : class
{
  private readonly IFactory<string, TConverter> _factory;

  public RecursiveConverterResolver(IFactory<string, TConverter> factory)
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
