using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToNative;

public abstract class BaseConversionToHost<TSpeckle, THost>
  : ISpeckleObjectToHostConversion,
    IRawConversion<TSpeckle, THost>
  where TSpeckle : Base
{
  public object Convert(Base target)
  {
    return RawConvert((TSpeckle)target)
      ?? throw new SpeckleConversionException($"ToRevit Converter of type {GetType()} returned null");
  }

  public abstract THost RawConvert(TSpeckle target);
}
