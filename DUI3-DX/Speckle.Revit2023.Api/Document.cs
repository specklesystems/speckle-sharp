using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Autodesk.Revit.DB;
using Speckle.ProxyGenerator;
using Speckle.Revit2023.Interfaces;
#pragma warning disable CA1010
#pragma warning disable CA1710

namespace Speckle.Revit2023.Api;

[Proxy(
  typeof(Document),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  new[] { "PlanTopology", "PlanTopologies", "TypeOfStorage", "Equals" }
)]
[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling")]
public partial interface IRevitDocumentProxy : IRevitDocument { }

[Proxy(
  typeof(ModelCurveArray),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  new[] { "GetEnumerator", "Item", "get_Item", "set_Item" }
)]
public partial interface IRevitModelCurveCollectionProxy : IRevitModelCurveCollection { }

public partial class ModelCurveArrayProxy
{
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public int Count => Size;

  public IEnumerator<IRevitModelCurve> GetEnumerator() =>
    new RevitModelCurveCollectionIterator(_Instance.ForwardIterator());

  private readonly struct RevitModelCurveCollectionIterator : IEnumerator<IRevitModelCurve>
  {
    private readonly ModelCurveArrayIterator _curveArrayIterator;

    public RevitModelCurveCollectionIterator(ModelCurveArrayIterator curveArrayIterator)
    {
      _curveArrayIterator = curveArrayIterator;
    }

    public void Dispose() => _curveArrayIterator.Dispose();

    public bool MoveNext() => _curveArrayIterator.MoveNext();

    public void Reset() => _curveArrayIterator.Reset();

    object IEnumerator.Current => Current;

    public IRevitModelCurve Current => new ModelCurveProxy((ModelCurve)_curveArrayIterator.Current);
  }

  public IRevitModelCurve this[int index]
  {
    get
    {
      var obj = _Instance.get_Item(index);
      return Mapster.TypeAdapter.Adapt<IRevitModelCurve>(obj);
    }
  }
}

[Proxy(typeof(ModelCurve), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitModelCurveProxy : IRevitModelCurve { }

[Proxy(typeof(CurveElement), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitCurveElementProxy : IRevitCurveElement { }

[Proxy(typeof(Curve), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitCurveProxy : IRevitCurve { }

[Proxy(typeof(XYZ), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitXYZProxy : IRevitXYZ { }

[Proxy(typeof(Units), ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface)]
public partial interface IRevitUnitsProxy : IRevitUnits { }

[Proxy(
  typeof(ForgeTypeId),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface,
  new[] { "Equals" }
)]
public partial interface IRevitForgeTypeIdProxy : IRevitForgeTypeId { }

[Proxy(
  typeof(FormatOptions),
  ImplementationOptions.UseExtendedInterfaces | ImplementationOptions.ProxyForBaseInterface
)]
public partial interface IRevitFormatOptionsProxy : IRevitFormatOptions { }
