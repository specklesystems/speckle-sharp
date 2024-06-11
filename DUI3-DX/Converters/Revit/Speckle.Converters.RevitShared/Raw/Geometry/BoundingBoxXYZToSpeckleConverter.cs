using Objects.Primitive;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public class BoundingBoxXYZToSpeckleConverter : ITypedConverter<IRevitBoundingBoxXYZ, SOG.Box>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<IRevitPlane, SOG.Plane> _planeConverter;
  private readonly IRevitPlaneUtils _revitPlaneUtils;

  public BoundingBoxXYZToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<IRevitPlane, SOG.Plane> planeConverter,
    IRevitPlaneUtils revitPlaneUtils
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _planeConverter = planeConverter;
    _revitPlaneUtils = revitPlaneUtils;
  }

  public SOG.Box Convert(IRevitBoundingBoxXYZ target)
  {
    // convert min and max pts to speckle first
    var min = _xyzToPointConverter.Convert(target.Min);
    var max = _xyzToPointConverter.Convert(target.Max);

    // get the base plane of the bounding box from the transform
    var transform = target.Transform;
    var plane = _revitPlaneUtils.CreateByOriginAndBasis(
      transform.Origin,
      transform.BasisX.Normalize(),
      transform.BasisY.Normalize()
    );

    var box = new SOG.Box()
    {
      xSize = new Interval(min.x, max.x),
      ySize = new Interval(min.y, max.y),
      zSize = new Interval(min.z, max.z),
      basePlane = _planeConverter.Convert(plane),
      units = _contextStack.Current.SpeckleUnits
    };

    return box;
  }
}
