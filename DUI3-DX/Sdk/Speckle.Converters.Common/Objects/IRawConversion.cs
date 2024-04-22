namespace Speckle.Converters.Common.Objects;

public interface IRawConversion<in TIn, out TOut>
{
  TOut RawConvert(TIn target);
}
