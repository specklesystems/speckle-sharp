using System.Drawing;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class PointCloudToHostConverter : ITypedConverter<SOG.Pointcloud, IRhinoPointCloud>
{
  private readonly ITypedConverter<IReadOnlyList<double>, IRhinoPoint3dList> _pointListConverter;
  private readonly IRhinoPointCloudFactory _rhinoPointCloudFactory;

  public PointCloudToHostConverter(ITypedConverter<IReadOnlyList<double>, IRhinoPoint3dList> pointListConverter, IRhinoPointCloudFactory rhinoPointCloudFactory)
  {
    _pointListConverter = pointListConverter;
    _rhinoPointCloudFactory = rhinoPointCloudFactory;
  }

  /// <summary>
  /// Converts raw Speckle point cloud data to Rhino PointCloud object.
  /// </summary>
  /// <param name="target">The raw Speckle Pointcloud object to convert.</param>
  /// <returns>The converted Rhino PointCloud object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public IRhinoPointCloud Convert(SOG.Pointcloud target)
  {
    var rhinoPoints = _pointListConverter.Convert(target.points);
    var rhinoPointCloud = _rhinoPointCloudFactory.Create(rhinoPoints);

    if (target.colors.Count == rhinoPoints.Count)
    {
      for (int i = 0; i < rhinoPoints.Count; i++)
      {
        rhinoPointCloud[i].Color = Color.FromArgb(target.colors[i]);
      }
    }

    return rhinoPointCloud;
  }
}
