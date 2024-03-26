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

  public RevitConverterToSpeckle(
    IIndex<Type, IHostObjectToSpeckleConversion> hostObjectConversions,
    ToSpeckleConvertedObjectsCache convertedObjectsCache
  )
  {
    _hostObjectConversions = hostObjectConversions;
    _convertedObjectsCache = convertedObjectsCache;
  }

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
    }

    return result;
  }

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
