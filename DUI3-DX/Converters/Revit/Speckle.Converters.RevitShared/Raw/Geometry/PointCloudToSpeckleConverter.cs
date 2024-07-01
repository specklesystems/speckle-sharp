using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class PointCloudToSpeckleConverter : ITypedConverter<DB.PointCloudInstance, SOG.Pointcloud>
{
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _xyzToPointConverter;
  private readonly ITypedConverter<DB.BoundingBoxXYZ, SOG.Box> _boundingBoxConverter;

  public PointCloudToSpeckleConverter(
    IRevitConversionContextStack contextStack,
    ITypedConverter<DB.XYZ, SOG.Point> xyzToPointConverter,
    ITypedConverter<DB.BoundingBoxXYZ, SOG.Box> boundingBoxConverter
  )
  {
    _contextStack = contextStack;
    _xyzToPointConverter = xyzToPointConverter;
    _boundingBoxConverter = boundingBoxConverter;
  }

  public SOG.Pointcloud Convert(DB.PointCloudInstance target)
  {
    var boundingBox = target.get_BoundingBox(null);
    using DB.Transform transform = target.GetTransform();
    {
      var minPlane = DB.Plane.CreateByNormalAndOrigin(DB.XYZ.BasisZ, transform.OfPoint(boundingBox.Min));
      var filter = DB.PointClouds.PointCloudFilterFactory.CreateMultiPlaneFilter(new List<DB.Plane>() { minPlane });
      var points = target.GetPoints(filter, 0.0001, 999999); // max limit is 1 mil but 1000000 throws error

      // POC: complaining about nullability
      var specklePointCloud = new SOG.Pointcloud
      {
        points = points
          .Select(o => _xyzToPointConverter.Convert(transform.OfPoint(o)))
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
