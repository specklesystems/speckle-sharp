namespace Speckle.Converters.Common;

public interface IProxyMap
{
  Type? GetMappedType(Type type);

  object CreateProxy(Type type, object toWrap);
}

// ghetto default interface implementation :(
public static class ProxyMapExtensions
{
  public static (Type, object)? WrapIfExists(this IProxyMap proxyMap, Type target, object toWrap)
  {
    var proxyType = proxyMap.GetMappedType(target);
    if (proxyType is not null)
    {
      return (proxyType, proxyMap.CreateProxy(proxyType, toWrap));
    }

    return null;
  }
}
