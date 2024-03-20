using Rhino;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Point = Speckle.Objects.Geometry.Point;

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

  public void Convert()
  {
    var objectConverter = _toSpeckle.ResolveInstance(nameof(Point));

    Console.WriteLine(objectConverter);
  }
}
