namespace Speckle.Converters.Common.Objects;

public interface ITypedConverter<TIn, TOut>
{
  TOut RawConvert(TIn target);
}
