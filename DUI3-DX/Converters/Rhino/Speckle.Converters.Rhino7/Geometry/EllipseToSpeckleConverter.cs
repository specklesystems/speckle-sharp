using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Objects.Primitives;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Ellipse), 0)]
public class EllipseToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Ellipse, SOG.Ellipse>
{
  private readonly IRawConversion<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;

  public EllipseToSpeckleConverter(
    IRawConversion<RG.Plane, SOG.Plane> planeConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter
  )
  {
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
  }

  public Base Convert(object target) => RawConvert((RG.Ellipse)target);

  public SOG.Ellipse RawConvert(RG.Ellipse target)
  {
    var nurbsCurve = target.ToNurbsCurve();
    return new(_planeConverter.RawConvert(target.Plane), target.Radius1, target.Radius2, Units.Meters)
    {
      domain = new Interval(0, 1),
      length = nurbsCurve.GetLength(),
      area = Math.PI * target.Radius1 * target.Radius2,
      bbox = _boxConverter.RawConvert(new RG.Box(nurbsCurve.GetBoundingBox(true)))
    };
  }
}
