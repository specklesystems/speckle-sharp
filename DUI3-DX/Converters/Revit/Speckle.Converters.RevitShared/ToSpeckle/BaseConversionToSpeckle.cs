using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: maybe but could be generic abstract Converters.Common?
public abstract class BaseConversionToSpeckle<THost, TSpeckle>
  : IHostObjectToSpeckleConversion,
    IRawConversion<THost, TSpeckle>
  where TSpeckle : Base
{
  public Base Convert(object target)
  {
    var result = RawConvert((THost)target);
    if (result is not Base @base)
    {
      throw new SpeckleConversionException(
        $"Expected resulting object to be {typeof(Base)} but was {result.GetType()}"
      );
    }

    return @base;
  }

  public abstract TSpeckle RawConvert(THost target);
}
