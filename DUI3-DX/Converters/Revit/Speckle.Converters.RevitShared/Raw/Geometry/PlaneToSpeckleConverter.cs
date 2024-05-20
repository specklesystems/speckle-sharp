using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PlaneToSpeckleConverter : ITypedConverter<DB.Plane, SOG.Plane>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<DB.XYZ, SOG.Vector> _xyzToVectorConverter;

  public PlaneToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<DB.XYZ, SOG.Vector> xyzToVectorConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _xyzToVectorConverter = xyzToVectorConverter;
  }

  public SOG.Plane RawConvert(DB.Plane target)
  {
    var origin = _xyzToPointConverter.RawConvert(target.Origin);
    var normal = _xyzToVectorConverter.RawConvert(target.Normal);
    var xdir = _xyzToVectorConverter.RawConvert(target.XVec);
    var ydir = _xyzToVectorConverter.RawConvert(target.YVec);

    return new SOG.Plane(origin, normal, xdir, ydir, _contextStack.Current.SpeckleUnits);
  }
}
