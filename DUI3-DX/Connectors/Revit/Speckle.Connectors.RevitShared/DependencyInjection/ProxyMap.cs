using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Connectors.Revit2023.Converters;

[SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline")]
public class ProxyMap : IProxyMap
{
  private static readonly ConcurrentDictionary<Type, Type> s_typeMaps = new();
  private static readonly ConcurrentDictionary<Type, Type> s_reverseMaps = new();
  private static readonly ConcurrentDictionary<Type, Func<object, object>> s_proxyFactory = new();

  [SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
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
    Add<DB.BasePoint, IRevitBasePoint>(x => new BasePointProxy(x));
    Add<DB.Wall, IRevitWall>(x => new WallProxy(x));
    Add<DB.Panel, IRevitPanel>(x => new PanelProxy(x));
    Add<DB.Floor, IRevitFloor>(x => new FloorProxy(x));
    Add<DB.Ceiling, IRevitCeiling>(x => new CeilingProxy(x));
    Add<DB.FootPrintRoof, IRevitFootPrintRoof>(x => new FootPrintRoofProxy(x));
    Add<DB.ModelLine, IRevitModelLine>(x => new ModelLineProxy(x));
    Add<DB.RoofBase, IRevitRoofBase>(x => new RoofBaseProxy(x));
  }

  private static void Add<T, TProxy>(Func<T, TProxy> f)
    where T : class
    where TProxy : notnull
  {
    s_typeMaps.TryAdd(typeof(T), typeof(TProxy));
    s_proxyFactory.TryAdd(typeof(TProxy), w => f((T)w));
    s_reverseMaps.TryAdd(typeof(TProxy), typeof(T));
  }

  public Type? GetMappedType(Type type)
  {
    if (s_typeMaps.TryGetValue(type, out var t))
    {
      return t;
    }
    return null;
  }

  public Type? UnmapType(Type type)
  {
    if (s_reverseMaps.TryGetValue(type, out var t))
    {
      return t;
    }
    return null;
  }

  public object CreateProxy(Type type, object toWrap) => s_proxyFactory[type](toWrap);
}
