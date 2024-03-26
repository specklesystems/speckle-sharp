using Autofac.Features.Indexed;
using System;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared;

// POC: maybe possible to restrict the access so this cannot be created directly?
public class RevitConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IIndex<Type, IHostObjectToSpeckleConversion> _hostObjectConversions;

  public RevitConverterToSpeckle(IIndex<Type, IHostObjectToSpeckleConversion> hostObjectConversions)
  {
    _hostObjectConversions = hostObjectConversions;
  }

  public Base Convert(object target)
  {
    var objectConverter =
      RetrieveObjectConversion(target.GetType())
      ?? throw new SpeckleConversionException($"Could not find conversion for object of type {target.GetType()}");

    return objectConverter.Convert(target)
      ?? throw new SpeckleConversionException($"Conversion of object with type {target.GetType()} returned null");

    //var objectConverter = _toSpeckle.ResolveInstance(nameof(Floor));

    //return objectConverter?.Convert(target)
    //  ?? throw new SpeckleConversionException("No converter or conversion returned null");
  }

  public IHostObjectToSpeckleConversion? RetrieveObjectConversion(Type objectType)
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
