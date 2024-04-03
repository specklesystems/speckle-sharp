using Speckle.Core.Models;

namespace Speckle.Converters.Common.Objects;

public interface IRawConversion<TIn, TOut>
{
  TOut RawConvert(TIn target);
}

// POC: this breaks the concept of IRawConversion be
// if we had this we would probably rename the interface as this is now a IToSpeckleRawConversion
public interface IRawConversion<TIn>
{
  Base ConvertToBase(TIn target);
}
