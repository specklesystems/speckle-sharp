using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public abstract class BaseConversionToSpeckle<THost, TSpeckle>
  : IHostObjectToSpeckleConversion,
    IRawConversion<THost, TSpeckle>
  where TSpeckle : Base
{
  public Base Convert(object target) => RawConvert((THost)target);

  public abstract TSpeckle RawConvert(THost target);
}
