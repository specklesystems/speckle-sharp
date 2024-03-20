using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.Geometry;

// POC: not sure I like the place of the default rank
[NameAndRankValue(nameof(Rhino.Geometry.Plane), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PlaneToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<RG.Plane, SOG.Plane>
{
  private readonly IRawConversion<RG.Vector3d, SOG.Vector> _vectorConverter;
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;

  public PlaneToSpeckleConverter(
    IRawConversion<RG.Vector3d, SOG.Vector> vectorConverter,
    IRawConversion<RG.Point3d, SOG.Point> pointConverter
  )
  {
    _vectorConverter = vectorConverter;
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((RG.Plane)target);

  public SOG.Plane RawConvert(RG.Plane target) =>
    new(
      _pointConverter.RawConvert(target.Origin),
      _vectorConverter.RawConvert(target.ZAxis),
      _vectorConverter.RawConvert(target.XAxis),
      _vectorConverter.RawConvert(target.YAxis),
      Units.Meters
    );
}
