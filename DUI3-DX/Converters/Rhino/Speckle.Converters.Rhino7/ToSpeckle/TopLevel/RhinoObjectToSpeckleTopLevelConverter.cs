using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public abstract class RhinoObjectToSpeckleTopLevelConverter<TTopLevelIn, TInRaw, TOutRaw> : IToSpeckleTopLevelConverter
  where TTopLevelIn : notnull
  where TInRaw : notnull
  where TOutRaw : Base
{
  private readonly ITypedConverter<TInRaw, TOutRaw> _conversion;

  protected RhinoObjectToSpeckleTopLevelConverter(ITypedConverter<TInRaw, TOutRaw> conversion)
  {
    _conversion = conversion;
  }

  // POC: IIndex would fix this as I would just request the type from `RhinoObject.Geometry` directly.
  protected abstract TInRaw GetTypedGeometry(TTopLevelIn input);

  public virtual Base Convert(object target)
  {
    var typedTarget = (TTopLevelIn)target; //can only be this typee
    var typedGeometry = GetTypedGeometry(typedTarget);

    var result = _conversion.Convert(typedGeometry);

    // POC: Any common operations for all RhinoObjects should be done here, not on the specific implementer
    // Things like user-dictionaries and other user-defined metadata.

    return result;
  }
}
