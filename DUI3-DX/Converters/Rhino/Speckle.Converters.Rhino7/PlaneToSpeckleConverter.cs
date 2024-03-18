using Rhino.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Objects.Geometry;
using Plane = Rhino.Geometry.Plane;
using Point = Speckle.Objects.Geometry.Point;

namespace Speckle.Converters.Rhino7;

public class PlaneToSpeckleConverter : IHostObjectToSpeckleConversion, IRawConversion<Plane, Objects.Geometry.Plane>
{
  private readonly IRawConversion<Vector3d, Vector> _vectorConverter;
  private readonly IRawConversion<Point3d, Point> _pointConverter;

  public PlaneToSpeckleConverter(
    IRawConversion<Vector3d, Vector> vectorConverter,
    IRawConversion<Point3d, Point> pointConverter
  )
  {
    _vectorConverter = vectorConverter;
    _pointConverter = pointConverter;
  }

  public Base Convert(object target) => RawConvert((Plane)target);

  public Objects.Geometry.Plane RawConvert(Plane target) =>
    new(
      _pointConverter.RawConvert(target.Origin),
      _vectorConverter.RawConvert(target.ZAxis),
      _vectorConverter.RawConvert(target.XAxis),
      _vectorConverter.RawConvert(target.YAxis)
    );
}
