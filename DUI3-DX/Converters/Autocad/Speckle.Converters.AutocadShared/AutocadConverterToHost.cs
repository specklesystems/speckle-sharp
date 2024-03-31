using System;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad;

public class AutocadConverterToHost : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;
  private readonly IHostToSpeckleUnitConverter<UnitsValue> _unitConverter;

  public AutocadConverterToHost(
    IFactory<string, ISpeckleObjectToHostConversion> toHost,
    IHostToSpeckleUnitConverter<UnitsValue> unitConverter
  )
  {
    _toHost = toHost;
    _unitConverter = unitConverter;
  }

  public object Convert(Base target)
  {
    if (target is not Base speckleObject)
    {
      throw new NotSupportedException(
        $"Conversion of {target.GetType().Name} to Host is not supported. Only objects that inherit from Base are."
      );
    }

    Type type = target.GetType();

    try
    {
      var objectConverter = _toHost.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
      }

      var convertedObject = objectConverter.Convert(speckleObject);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
