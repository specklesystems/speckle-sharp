using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PlaneConverterToHost : ITypedConverter<SOG.Plane, DB.Plane>
{
  private readonly ITypedConverter<SOG.Point, DB.XYZ> _pointConverter;
  private readonly ITypedConverter<SOG.Vector, DB.XYZ> _vectorConverter;

  public PlaneConverterToHost(
    ITypedConverter<SOG.Point, XYZ> pointConverter,
    ITypedConverter<Vector, XYZ> vectorConverter
  )
  {
    _pointConverter = pointConverter;
    _vectorConverter = vectorConverter;
  }

  public DB.Plane Convert(SOG.Plane target) =>
    DB.Plane.CreateByOriginAndBasis(
      _pointConverter.Convert(target.origin),
      _vectorConverter.Convert(target.xdir).Normalize(),
      _vectorConverter.Convert(target.ydir).Normalize()
    );
}
