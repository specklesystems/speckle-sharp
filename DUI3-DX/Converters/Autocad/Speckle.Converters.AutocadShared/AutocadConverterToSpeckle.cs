using System;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad;

public class AutocadConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;
  private readonly IHostToSpeckleUnitConverter<UnitsValue> _unitConverter;

  public AutocadConverterToSpeckle(
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle,
    IHostToSpeckleUnitConverter<UnitsValue> unitConverter
  )
  {
    _toSpeckle = toSpeckle;
    _unitConverter = unitConverter;
  }

  public Base Convert(object target)
  {
    if (target is not DBObject dbObject)
    {
      throw new NotSupportedException(
        $"Conversion of {target.GetType().Name} to Speckle is not supported. Only objects that inherit from DBObject are."
      );
    }

    Type type = dbObject.GetType();

    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
      }

      var convertedObject = objectConverter.Convert(dbObject);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
