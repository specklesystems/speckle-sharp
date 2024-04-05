using Autofac.Features.Indexed;
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
    var objectConverter = _toSpeckle.ResolveInstance(target.GetType().Name);

    if (objectConverter == null)
    {
      throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
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
}
