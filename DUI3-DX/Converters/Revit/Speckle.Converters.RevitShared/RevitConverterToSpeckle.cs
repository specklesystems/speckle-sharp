using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared;

// POC: maybe possible to restrict the access so this cannot be created directly?
public class RevitConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public RevitConverterToSpeckle(
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle,
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _toSpeckle = toSpeckle;
    _convertedObjectsCache = convertedObjectsCache;
    _parameterValueExtractor = parameterValueExtractor;
  }

  // POC: our assumption here is target is valid for conversion
  // if it cannot be converted then we should throw
  public Base Convert(object target)
  {
    var objectConverter = GetConversionForObject(target.GetType());

    if (objectConverter == null)
    {
      throw new SpeckleConversionException($"No conversion found for {target.GetType().Name}");
    }

    Base result =
      objectConverter.Convert(target)
      ?? throw new SpeckleConversionException($"Conversion of object with type {target.GetType()} returned null");

    // POC : where should logic common to most objects go?
    if (target is Element element)
    {
      _convertedObjectsCache.AddConvertedBase(element.UniqueId, result);
      _parameterValueExtractor.RemoveUniqueId(element.UniqueId);
    }

    return result;
  }

  // POC: consider making this a more accessible as a pattern to other connectors
  // https://spockle.atlassian.net/browse/CNX-9397
  private IHostObjectToSpeckleConversion? GetConversionForObject(Type objectType)
  {
    if (objectType == typeof(object))
    {
      return null;
    }

    if (_toSpeckle.ResolveInstance(objectType.Name) is IHostObjectToSpeckleConversion conversion)
    {
      return conversion;
    }

    return GetConversionForObject(objectType.BaseType);
  }
}
