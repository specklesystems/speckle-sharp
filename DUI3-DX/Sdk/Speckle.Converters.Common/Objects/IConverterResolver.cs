namespace Speckle.Converters.Common.Objects;

public interface IConverterResolver<out TConverter>
  where TConverter : class
{
  public TConverter? GetConversionForType(Type objectType);
}
