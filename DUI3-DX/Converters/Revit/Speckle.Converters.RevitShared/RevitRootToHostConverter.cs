using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared;

public class RevitRootToHostConverter : IRootToHostConverter
{
  private readonly IConverterResolver<IToHostTopLevelConverter> _converterResolver;

  public RevitRootToHostConverter(IConverterResolver<IToHostTopLevelConverter> converterResolver)
  {
    _converterResolver = converterResolver;
  }

  public object Convert(Base target)
  {
    var objectConverter = _converterResolver.GetConversionForType(target.GetType());

    if (objectConverter == null)
    {
      throw new SpeckleConversionException($"No conversion found for {target.GetType().Name}");
    }

    return objectConverter.Convert(target)
      ?? throw new SpeckleConversionException($"Conversion of object with type {target.GetType()} returned null");
  }
}
