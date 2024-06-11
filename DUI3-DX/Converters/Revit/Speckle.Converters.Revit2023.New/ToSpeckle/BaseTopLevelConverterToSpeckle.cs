using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Revit2023.ToSpeckle;

// POC: maybe but could be generic abstract Converters.Common?
// or maybe it's not actually doing very much now and can come out
public abstract class BaseTopLevelConverterToSpeckle<THost, TSpeckle>
  : IToSpeckleTopLevelConverter,
    // POC: why do we need to do this for each base conversion?
    ITypedConverter<THost, TSpeckle>
  where TSpeckle : Base
{
  public Base Convert(object target)
  {
    var result = Convert((THost)target);

    // POC: unless I am going bonkers, we've constrained TSpeckle to Base
    // so it should always BE base?
    if (result is not Base @base)
    {
      throw new SpeckleConversionException(
        $"Expected resulting object to be {typeof(Base)} but was {result.GetType()}"
      );
    }

    return @base;
  }

  public abstract TSpeckle Convert(THost target);
}
