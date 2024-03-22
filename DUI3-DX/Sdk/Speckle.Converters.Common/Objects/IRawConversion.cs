using Speckle.Core.Models;

namespace Speckle.Converters.Common.Objects;

public interface IRawConversion<TIn, TOut>
{
  TOut RawConvert(TIn target);
}

public interface IRawConversion<TIn>
{
  Base ConvertToBase(TIn target);
}
