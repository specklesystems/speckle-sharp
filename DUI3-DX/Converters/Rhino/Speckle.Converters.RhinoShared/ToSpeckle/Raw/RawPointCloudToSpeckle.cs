using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class RawPointCloudToSpeckle : ITypedConverter<RG.PointCloud, SOG.Pointcloud>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;

  public RawPointCloudToSpeckle(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<RG.Box, SOG.Box> boxConverter
  )
  {
    _contextStack = contextStack;
    _boxConverter = boxConverter;
  }

  /// <summary>
  /// Converts a Rhino PointCloud object to a Speckle Pointcloud object.
  /// </summary>
  /// <param name="target">The Rhino PointCloud object to convert.</param>
  /// <returns>The converted Speckle Pointcloud object.</returns>
  public SOG.Pointcloud Convert(RG.PointCloud target) =>
    new()
    {
      points = target.GetPoints().SelectMany(pt => new[] { pt.X, pt.Y, pt.Z }).ToList(),
      colors = target.GetColors().Select(o => o.ToArgb()).ToList(),
      bbox = _boxConverter.Convert(new RG.Box(target.GetBoundingBox(true))),
      units = _contextStack.Current.SpeckleUnits
    };
}
