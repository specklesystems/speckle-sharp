using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

using Speckle.Objects.Geometry;

namespace Speckle.Converters.ArcGIS3;

public class ArcGISConverterToSpeckle : ISpeckleConverterToSpeckle
{
  private readonly IFactory<string, IHostObjectToSpeckleConversion> _toSpeckle;

  public ArcGISConverterToSpeckle(IFactory<string, IHostObjectToSpeckleConversion> toSpeckle)
  {
    _toSpeckle = toSpeckle;
  }

  public void Convert()
  {
    var objectConverter = _toSpeckle.ResolveInstance(nameof(Point));
    Console.WriteLine(objectConverter);
  }
}
