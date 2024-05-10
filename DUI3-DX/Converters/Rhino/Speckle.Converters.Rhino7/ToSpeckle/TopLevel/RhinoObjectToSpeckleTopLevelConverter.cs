using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.TopLevel;

public abstract class RhinoObjectToSpeckleTopLevelConverter<TTopLevelIn, TInRaw, TOutRaw>
  : IHostObjectToSpeckleConversion
  where TTopLevelIn : Rhino.DocObjects.RhinoObject
  where TInRaw : RG.GeometryBase
  where TOutRaw : Base
{
  public IRawConversion<TInRaw, TOutRaw> Conversion { get; }

  protected RhinoObjectToSpeckleTopLevelConverter(IRawConversion<TInRaw, TOutRaw> conversion)
  {
    Conversion = conversion;
  }

  // POC: IIndex would fix this as I would just request the type from `RhinoObject.Geometry` directly.
  protected abstract TInRaw GetTypedGeometry(TTopLevelIn input);

  public virtual Base Convert(object target)
  {
    var typedTarget = (TTopLevelIn)target;
    var typedGeometry = GetTypedGeometry(typedTarget);

    var result = Conversion.RawConvert(typedGeometry);

    // POC: Any common operations for all RhinoObjects should be done here, not on the specific implementer
    // Things like user-dictionaries and other user-defined metadata.

    return result;
  }
}
