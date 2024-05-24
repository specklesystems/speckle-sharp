using System.Collections;
using Autodesk.Revit.DB;
using Speckle.ProxyGenerator;
using Speckle.Revit2023.Interfaces;

namespace Speckle.Revit2023.Api;

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
