using Speckle.Converters.Common;
using Speckle.ProxyGenerator;

namespace Speckle.Connectors.Rhino7.DependencyInjection;

public class ProxyMapper : IProxyMapper
{
  public Type? GetMappedTypeFromHostType(Type type) => ProxyMap.GetMappedTypeFromHostType(type);

  public Type? GetMappedTypeFromProxyType(Type type) => ProxyMap.GetMappedTypeFromProxyType(type);

  public Type? GetHostTypeFromMappedType(Type type) => ProxyMap.GetHostTypeFromMappedType(type);

  public object CreateProxy(Type type, object toWrap) => ProxyMap.CreateProxy(type, toWrap);

  public T CreateProxy<T>(object toWrap) => ProxyMap.CreateProxy<T>(toWrap);
}
