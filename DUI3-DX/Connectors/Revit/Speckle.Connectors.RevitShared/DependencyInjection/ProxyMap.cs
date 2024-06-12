using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Speckle.Converters.Common;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Connectors.Revit2023.Converters;

[SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline")]
public class ProxyMap : IProxyMap
{
  private static readonly ConcurrentDictionary<Type, Type> s_typeMaps = new();
  private static readonly ConcurrentDictionary<Type, Func<object, object>> s_proxyFactory = new();

  static ProxyMap()
  {
    Add<DB.Element, IRevitElement>(x => new ElementProxy(x));
    Add<DB.FamilyInstance, IRevitFamilyInstance>(x => new FamilyInstanceProxy(x));
    Add<DB.Curve, IRevitCurve>(x => new CurveProxy(x));
    Add<DB.BoundarySegment, IRevitBoundarySegment>(x => new BoundarySegmentProxy(x));
    Add<DB.Level, IRevitLevel>(x => new LevelProxy(x));
    Add<DB.Location, IRevitLocation>(x => new LocationProxy(x));
    Add<DB.Material, IRevitMaterial>(x => new MaterialProxy(x));
    Add<DB.ModelCurveArray, IRevitModelCurveArray>(x => new ModelCurveArrayProxy(x));
    Add<DB.ModelCurveArrArray, IRevitModelCurveArrArray>(x => new ModelCurveArrArrayProxy(x));
    Add<DB.Parameter, IRevitParameter>(x => new ParameterProxy(x));
  }

  private static void Add<T, TProxy>(Func<T, TProxy> f)
    where T : class
    where TProxy : notnull
  {
    s_typeMaps.TryAdd(typeof(T), typeof(TProxy));
    s_proxyFactory.TryAdd(typeof(TProxy), w => f((T)w));
  }

  public Type? GetMappedType(Type type)
  {
    if (s_typeMaps.TryGetValue(type, out var t))
    {
      return t;
    }
    return null;
  }

  public object CreateProxy(Type type, object toWrap) => s_proxyFactory[type](toWrap);
}
