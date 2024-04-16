using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared;

public class RevitConverterToHost : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHostConversions;

  public RevitConverterToHost(IFactory<string, ISpeckleObjectToHostConversion> toHostConversions)
  {
    _toHostConversions = toHostConversions;
  }

  public object Convert(Base target)
  {
    ISpeckleObjectToHostConversion conversion =
      GetToHostConversion(target.GetType())
      ?? throw new SpeckleConversionException($"No conversion found for {target.GetType().Name}");

    object result =
      conversion.Convert(target)
      ?? throw new SpeckleConversionException($"Conversion of object with type {target.GetType()} returned null");

    return result;
  }

  private ISpeckleObjectToHostConversion? GetToHostConversion(Type? targetType)
  {
    if (targetType is null || targetType == typeof(object))
    {
      return null;
    }

    if (_toHostConversions.ResolveInstance(targetType.Name) is ISpeckleObjectToHostConversion conversion)
    {
      return conversion;
    }

    return GetToHostConversion(targetType.BaseType);
  }
}
