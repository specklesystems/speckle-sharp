using Objects;
using Objects.Primitive;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PlaneToSpeckleConverter : IRawConversion<DB.Plane, SOG.Plane>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly IRawConversion<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly IRawConversion<DB.XYZ, SOG.Vector> _xyzToVectorConverter;

  public PlaneToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    IRawConversion<DB.XYZ, SOG.Point> xyzToPointConverter,
    IRawConversion<DB.XYZ, SOG.Vector> xyzToVectorConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _xyzToPointConverter = xyzToPointConverter;
  }

  public SOG.Plane RawConvert(DB.Plane plane)
  {
    var origin = _xyzToPointConverter.RawConvert(plane.Origin);
    var normal = _xyzToVectorConverter.RawConvert(plane.Normal);
    var xdir = _xyzToVectorConverter.RawConvert(plane.XVec);
    var ydir = _xyzToVectorConverter.RawConvert(plane.YVec);

    return new SOG.Plane(origin, normal, xdir, ydir, _contextStack.Current.SpeckleUnits);
  }
}
