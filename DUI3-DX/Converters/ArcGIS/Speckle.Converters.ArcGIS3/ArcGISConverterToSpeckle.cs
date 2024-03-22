using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3;

public class ArcGISConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;

  public ArcGISConverterToSpeckle(IFactory<string, IHostObjectToSpeckleConversion> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public Base Convert(object target)
  {
    Type type = typeof(String);

    try
    {
      var objectConverter = _toSpeckle.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
      }

      var convertedObject = objectConverter.Convert("Teststt");

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // Just rethrowing for now, Logs may be needed here.
    }
  }
}
