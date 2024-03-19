using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Objects.Primitives;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Circle), 0)]
public class CircleToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Circle, SOG.Circle>
{
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;

  public CircleToSpeckleConverter(IRawConversion<RG.Plane, SOG.Plane> planeConverter)
  {
    _planeConverter = planeConverter;
  }

  public Base Convert(object target) => RawConvert((RG.Circle)target);

  public SOG.Circle RawConvert(RG.Circle target) =>
    new(_planeConverter.RawConvert(target.Plane), target.Radius, Units.Meters)
    {
      domain = new Interval(0, 1),
      length = 2 * Math.PI * target.Radius,
      area = Math.PI * Math.Pow(target.Radius, 2)
    };
}
