using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToHost.Geometry;

public class PlaneToHostGeometryConverter : IRawConversion<SOG.Plane, AG.Plane>
{
  private readonly IRawConversion<SOG.Point, AG.Point3d> _pointConverter;
  private readonly IRawConversion<SOG.Vector, AG.Vector3d> _vectorConverter;

  public PlaneToHostGeometryConverter(
    IRawConversion<SOG.Point, AG.Point3d> pointConverter,
    IRawConversion<SOG.Vector, AG.Vector3d> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  public object Convert(Base target) => RawConvert((SOG.Plane)target);

  public AG.Plane RawConvert(SOG.Plane target) =>
    new(_pointConverter.RawConvert(target.origin), _vectorConverter.RawConvert(target.normal));
}
