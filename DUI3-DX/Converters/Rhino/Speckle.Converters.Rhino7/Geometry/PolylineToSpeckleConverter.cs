using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

[NameAndRankValue(nameof(RG.Polyline), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PolylineToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Polyline, SOG.Polyline>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;

  public PolylineToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
  }

  public Base Convert(object target) => RawConvert((RG.Polyline)target);

  public SOG.Polyline RawConvert(RG.Polyline target)
  {
    var box = _boxConverter.RawConvert(new RG.Box(target.BoundingBox));
    var points = target.Select(pt => _pointConverter.RawConvert(pt)).ToList();

    if (target.IsClosed)
    {
      points.RemoveAt(points.Count - 1);
    }

    return new SOG.Polyline(points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(), Units.Meters)
    {
      // TODO: Find out why original polyline conversion has `Interval` input.
      bbox = box,
      length = target.Length
    };
  }
}
