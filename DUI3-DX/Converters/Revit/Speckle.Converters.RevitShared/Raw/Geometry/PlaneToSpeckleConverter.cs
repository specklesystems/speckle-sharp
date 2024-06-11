using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class PlaneToSpeckleConverter : ITypedConverter<IRevitPlane, SOG.Plane>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<IRevitXYZ, SOG.Vector> _xyzToVectorConverter;

  public PlaneToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<IRevitXYZ, SOG.Vector> xyzToVectorConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _xyzToVectorConverter = xyzToVectorConverter;
  }

  public SOG.Plane Convert(IRevitPlane target)
  {
    var origin = _xyzToPointConverter.Convert(target.Origin);
    var normal = _xyzToVectorConverter.Convert(target.Normal);
    var xdir = _xyzToVectorConverter.Convert(target.XVec);
    var ydir = _xyzToVectorConverter.Convert(target.YVec);

    return new SOG.Plane(origin, normal, xdir, ydir, _contextStack.Current.SpeckleUnits);
  }
}
