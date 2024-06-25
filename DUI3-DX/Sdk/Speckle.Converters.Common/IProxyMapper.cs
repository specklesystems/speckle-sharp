namespace Speckle.Converters.Common;

public interface IProxyMapper
{
  Type? GetMappedTypeFromHostType(Type type);
  Type? GetMappedTypeFromProxyType(Type type);
  Type? GetHostTypeFromMappedType(Type type);

  object CreateProxy(Type type, object toWrap);

  T CreateProxy<T>(object toWrap);
}

public record WrappedType(Type Type, object Target);

// ghetto default interface implementation :(
public static class ProxyMapExtensions
{
  public static WrappedType? WrapIfExists(this IProxyMapper proxyMap, Type target, object toWrap)
  {
    var mappedType = proxyMap.GetMappedTypeFromHostType(target);
    if (mappedType is not null)
    {
      return new(mappedType, proxyMap.CreateProxy(mappedType, toWrap));
    }
    mappedType = proxyMap.GetMappedTypeFromProxyType(target);
    if (mappedType is not null)
    {
      return new(mappedType, toWrap);
    }
    return null;
  }
}
