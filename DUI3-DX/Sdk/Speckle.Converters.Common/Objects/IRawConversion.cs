namespace Speckle.Converters.Common.Objects;

public interface IRawConversion<TIn, TOut>
{
  TOut RawConvert(TIn target);
}
