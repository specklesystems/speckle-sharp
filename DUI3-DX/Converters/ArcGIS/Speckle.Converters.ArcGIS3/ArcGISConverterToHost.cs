using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3;

public class ArcGISConverterToHost : ISpeckleConverterToHost
{
  private readonly IFactory<string, ISpeckleObjectToHostConversion> _toHost;

  public ArcGISConverterToHost(IFactory<string, ISpeckleObjectToHostConversion> toHost)
  {
    _toHost = toHost;
  }

  public object Convert(Base target)
  {
    Type type = target.GetType();

    try
    {
      var objectConverter = _toHost.ResolveInstance(type.Name);

      if (objectConverter == null)
      {
        throw new NotSupportedException($"No conversion found for {target.GetType().Name}");
      }

      var convertedObject = objectConverter.Convert(target);

      return convertedObject;
    }
    catch (SpeckleConversionException e)
    {
      Console.WriteLine(e);
      throw; // POC: Just rethrowing for now, Logs may be needed here.
    }
  }
}
