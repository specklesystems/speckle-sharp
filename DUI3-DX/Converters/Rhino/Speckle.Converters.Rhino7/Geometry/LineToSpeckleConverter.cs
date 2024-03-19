using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Line), 0)]
public class LineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Line, SOG.Line>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;

  public LineToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    // TODO: Missing context resolver/factory
  }

  public Base Convert(object target) => RawConvert((RG.Line)target);

  public SOG.Line RawConvert(RG.Line target) =>
    new(_pointConverter.RawConvert(target.From), _pointConverter.RawConvert(target.To), Units.Meters)
    {
      length = target.Length,
      domain = new Objects.Primitives.Interval(0, target.Length),
      bbox = _boxConverter.RawConvert(new RG.Box(target.BoundingBox))
    };
}
