using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class BoundingBoxXYZToSpeckleConverter : ITypedConverter<DB.BoundingBoxXYZ, SOG.Box>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<DB.Plane, SOG.Plane> _planeConverter;

  public BoundingBoxXYZToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<DB.Plane, SOG.Plane> planeConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _planeConverter = planeConverter;
  }

  public SOG.Box RawConvert(DB.BoundingBoxXYZ target)
  {
    // convert min and max pts to speckle first
    var min = _xyzToPointConverter.RawConvert(target.Min);
    var max = _xyzToPointConverter.RawConvert(target.Max);

    // get the base plane of the bounding box from the transform
    var transform = target.Transform;
    var plane = DB.Plane.CreateByOriginAndBasis(
      transform.Origin,
      transform.BasisX.Normalize(),
      transform.BasisY.Normalize()
    );

    var box = new SOG.Box()
    {
      xSize = new Interval(min.x, max.x),
      ySize = new Interval(min.y, max.y),
      zSize = new Interval(min.z, max.z),
      basePlane = _planeConverter.RawConvert(plane),
      units = _contextStack.Current.SpeckleUnits
    };

    return box;
  }
}
