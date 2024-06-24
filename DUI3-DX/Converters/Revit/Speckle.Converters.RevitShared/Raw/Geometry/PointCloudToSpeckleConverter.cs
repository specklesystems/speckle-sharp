using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PointCloudToSpeckleConverter : ITypedConverter<IRevitPointCloudInstance, SOG.Pointcloud>
{
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<IRevitBoundingBoxXYZ, SOG.Box> _boundingBoxConverter;
  private readonly IRevitPlaneUtils _revitPlaneUtils;
  private readonly IRevitXYZUtils _revitxyzUtils;
  private readonly IRevitFilterFactory _revitFilterFactory;

  public PointCloudToSpeckleConverter(
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    ITypedConverter<IRevitXYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<IRevitBoundingBoxXYZ, SOG.Box> boundingBoxConverter,
    IRevitPlaneUtils revitPlaneUtils,
    IRevitXYZUtils revitxyzUtils,
    IRevitFilterFactory revitFilterFactory
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _boundingBoxConverter = boundingBoxConverter;
    _revitPlaneUtils = revitPlaneUtils;
    _revitxyzUtils = revitxyzUtils;
    _revitFilterFactory = revitFilterFactory;
  }

  public SOG.Pointcloud Convert(IRevitPointCloudInstance target)
  {
    var boundingBox = target.GetBoundingBox();
    using IRevitTransform transform = target.GetTransform();
    {
      var minPlane = _revitPlaneUtils.CreateByNormalAndOrigin(
        _revitxyzUtils.BasisZ,
        transform.OfPoint(boundingBox.NotNull().Min)
      );
      var filter = _revitFilterFactory.CreateMultiPlaneFilter(minPlane);
      var points = target.GetPoints(filter, 0.0001, 999999); // max limit is 1 mil but 1000000 throws error

      // POC: complaining about nullability
      var specklePointCloud = new SOG.Pointcloud
      {
        points = points
          .Select(o => _xyzToPointConverter.Convert(transform.OfPoint(o.ToXYZ())))
          .SelectMany(o => new List<double>() { o.x, o.y, o.z })
          .ToList(),
        colors = points.Select(o => o.Color).ToList(),
        units = _contextStack.Current.SpeckleUnits,
        bbox = _boundingBoxConverter.Convert(boundingBox)
      };

      return specklePointCloud;
    }
  }
}
