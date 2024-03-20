using Rhino;
using Rhino.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Point = Speckle.Objects.Geometry.Point;

namespace Speckle.Converters.Rhino7;

// POC: not sure I like the place of the default rank
[NameAndRankValue(nameof(Rhino.Geometry.Point), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Point3d, Point>
{
  private readonly IConversionContext<RhinoDoc, UnitSystem> _conversionContext;

  public PointToSpeckleConverter(IConversionContext<RhinoDoc, UnitSystem> conversionContext)
  {
    _conversionContext = conversionContext;
  }

  public Base Convert(object target) => RawConvert((Point3d)target);

  public Point RawConvert(Point3d target) => new(target.X, target.Y, target.Z, _conversionContext.SpeckleUnits);
}
