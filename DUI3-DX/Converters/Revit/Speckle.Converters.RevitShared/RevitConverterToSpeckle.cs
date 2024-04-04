using Autofac.Features.Indexed;
using System;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Autodesk.Revit.DB;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared;

// POC: maybe possible to restrict the access so this cannot be created directly?
public class RevitConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IIndex<Type, IHostObjectToSpeckleConversion> _hostObjectConversions;
  private readonly ToSpeckleConvertedObjectsCache _convertedObjectsCache;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public RevitConverterToSpeckle(
    IIndex<Type, IHostObjectToSpeckleConversion> hostObjectConversions,
    ToSpeckleConvertedObjectsCache convertedObjectsCache,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _hostObjectConversions = hostObjectConversions;
    _convertedObjectsCache = convertedObjectsCache;
    _parameterValueExtractor = parameterValueExtractor;
  }

  // POC: our assumption here is target is valid for conversion
  // if it cannot be converted then we should throw
  public Base Convert(object target)
  {
    var objectConverter =
      RetrieveObjectConversion(target.GetType())
      ?? throw new SpeckleConversionException($"Could not find conversion for object of type {target.GetType()}");

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

  // POC: we should try to de-couple raw object conversion and paramater scraping so they can happen..
  //   "yeah, I'm wondering if we can make conversion "pipelines" or something
  //    like that where we could add all the Wall properties like length and height
  //    and then continue passing the object down to more generic conversions that
  //    could add more generic info like parameters or display value."
  //
  // We need to look for the commonality...
  //   "I think it's achievable and makes more sense than having to add parameters on every conversion"
  private IHostObjectToSpeckleConversion? RetrieveObjectConversion(Type objectType)
  {
    if (_hostObjectConversions.TryGetValue(objectType, out var conversion))
    {
      return conversion;
    }

    if (objectType.BaseType == typeof(object))
    {
      return null;
    }
    return RetrieveObjectConversion(objectType.BaseType);
  }
}
