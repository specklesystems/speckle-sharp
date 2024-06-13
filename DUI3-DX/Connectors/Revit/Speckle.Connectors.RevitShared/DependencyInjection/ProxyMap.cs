using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Connectors.Revit2023.Converters;

[SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline")]
public class ProxyMap : IProxyMap
{
  private static readonly ConcurrentDictionary<Type, Type> s_revitToInterfaceMap = new();
  private static readonly ConcurrentDictionary<Type, Type> s_proxyToInterfaceMap = new();
  private static readonly ConcurrentDictionary<Type, Type> s_interfaceToRevit = new();
  private static readonly ConcurrentDictionary<Type, Func<object, object>> s_proxyFactory = new();

  [SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
  static ProxyMap()
  {
    Add<DB.Element, IRevitElement, ElementProxy>(x => new ElementProxy(x));
    Add<DB.FamilyInstance, IRevitFamilyInstance, FamilyInstanceProxy>(x => new FamilyInstanceProxy(x));
    Add<DB.Curve, IRevitCurve, CurveProxy>(x => new CurveProxy(x));
    Add<DB.BoundarySegment, IRevitBoundarySegment, BoundarySegmentProxy>(x => new BoundarySegmentProxy(x));
    Add<DB.Level, IRevitLevel, LevelProxy>(x => new LevelProxy(x));
    Add<DB.Location, IRevitLocation, LocationProxy>(x => new LocationProxy(x));
    Add<DB.Material, IRevitMaterial, MaterialProxy>(x => new MaterialProxy(x));
    Add<DB.ModelCurveArray, IRevitModelCurveArray, ModelCurveArrayProxy>(x => new ModelCurveArrayProxy(x));
    Add<DB.ModelCurveArrArray, IRevitModelCurveArrArray, ModelCurveArrArrayProxy>(x => new ModelCurveArrArrayProxy(x));
    Add<DB.Parameter, IRevitParameter, ParameterProxy>(x => new ParameterProxy(x));
    Add<DB.BasePoint, IRevitBasePoint, BasePointProxy>(x => new BasePointProxy(x));
    Add<DB.Wall, IRevitWall, WallProxy>(x => new WallProxy(x));
    Add<DB.Panel, IRevitPanel, PanelProxy>(x => new PanelProxy(x));
    Add<DB.Floor, IRevitFloor, FloorProxy>(x => new FloorProxy(x));
    Add<DB.Ceiling, IRevitCeiling, CeilingProxy>(x => new CeilingProxy(x));
    Add<DB.FootPrintRoof, IRevitFootPrintRoof, FootPrintRoofProxy>(x => new FootPrintRoofProxy(x));
    Add<DB.ModelLine, IRevitModelLine, ModelLineProxy>(x => new ModelLineProxy(x));
    Add<DB.RoofBase, IRevitRoofBase, RoofBaseProxy>(x => new RoofBaseProxy(x));
  }

  private static void Add<T, TInterface, TProxy>(Func<T, TProxy> f)
    where T : class
    where TInterface : notnull
    where TProxy : TInterface
  {
    s_revitToInterfaceMap.TryAdd(typeof(T), typeof(TInterface));
    s_proxyToInterfaceMap.TryAdd(typeof(TProxy), typeof(TInterface));
    s_proxyFactory.TryAdd(typeof(TInterface), w => f((T)w));
    s_interfaceToRevit.TryAdd(typeof(TInterface), typeof(T));
  }

  public Type? GetMappedTypeFromHostType(Type type)
  {
    if (s_revitToInterfaceMap.TryGetValue(type, out var t))
    {
      return t;
    }
    return null;
  } 
  public Type? GetMappedTypeFromProxyType(Type type)
  {
    if (s_proxyToInterfaceMap.TryGetValue(type, out var t))
    {
      return t;
    }

    return null;
  }

  public Type? GetHostTypeFromMappedType(Type type)
  {
    if (s_interfaceToRevit.TryGetValue(type, out var t))
    {
      return t;
    }

    return null;
  }

  public object CreateProxy(Type type, object toWrap) => s_proxyFactory[type](toWrap);
}
