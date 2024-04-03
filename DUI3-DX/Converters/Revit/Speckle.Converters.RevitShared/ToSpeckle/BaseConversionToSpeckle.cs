using System.Collections.Generic;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: maybe but could be generic abstract Converters.Common?
public abstract class BaseConversionToSpeckle<THost, TSpeckle>
  : IHostObjectToSpeckleConversion,
    IRawConversion<THost>,
    IRawConversion<THost, TSpeckle>
  where TSpeckle : notnull
{
  public Base Convert(object target)
  {
    return ConvertToBase((THost)target);
  }

  public Base ConvertToBase(THost target)
  {
    TSpeckle conversionResult = RawConvert(target);
    return conversionResult as Base
      ?? throw new SpeckleConversionException($"Unable to cast conversion result of type {conversionResult.GetType()}");
  }

  public abstract TSpeckle RawConvert(THost target);

  // POC: this isn't even used, needs review, not sure we need it atm?
  public IEnumerable<TSpeckle> RawConvertMany(IEnumerable<THost> target)
  {
    foreach (var targetItem in target)
    {
      yield return RawConvert(targetItem);
    }
  }
}
