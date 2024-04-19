namespace Speckle.Converters.Common.Objects;

public interface IConditionalToSpeckleConverter<TIn> : IToSpeckleConverter<TIn>
{
  /// <summary>
  /// Will return true if the provided object meets the conditions required for the converter to convert the object
  /// </summary>
  /// <param name="target"></param>
  /// <returns></returns>
  bool CanConvert(TIn target);
}
