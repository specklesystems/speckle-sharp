using Rhino;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7;

public class RhinoConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;
  private readonly IHostToSpeckleUnitConverter<UnitSystem> _unitConverter;

  public RhinoConverterToSpeckle(
    IFactory<string, IHostObjectToSpeckleConversion> toSpeckle,
    IHostToSpeckleUnitConverter<UnitSystem> unitConverter
  )
  {
    _toSpeckle = toSpeckle;
    _unitConverter = unitConverter;
  }

  public Base Convert(object target)
  {
    if (target is not RhinoObject rhinoObject)
    {
      throw new NotSupportedException(
        $"Conversion of {target.GetType().Name} to Speckle is not supported. Only objects that inherit from RhinoObject are."
      );
    }

    Type type = rhinoObject.Geometry.GetType();

    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
      }

      var convertedObject = objectConverter.Convert(rhinoObject.Geometry);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
