namespace Speckle.Converters.Common.Objects;

public interface ITypedConverter<in TIn, out TOut>
{
  TOut Convert(TIn target);
}
