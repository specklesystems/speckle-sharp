using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class RawPointCloudToSpeckle : ITypedConverter<IRhinoPointCloud, SOG.Pointcloud>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public RawPointCloudToSpeckle(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter, IRhinoBoxFactory rhinoBoxFactory)
  {
    _contextStack = contextStack;
    _boxConverter = boxConverter;
    _rhinoBoxFactory = rhinoBoxFactory;
  }

  /// <summary>
  /// Converts a Rhino PointCloud object to a Speckle Pointcloud object.
  /// </summary>
  /// <param name="target">The Rhino PointCloud object to convert.</param>
  /// <returns>The converted Speckle Pointcloud object.</returns>
  public SOG.Pointcloud Convert(IRhinoPointCloud target) =>
    new()
    {
      points = target.GetPoints().SelectMany(pt => new[] { pt.X, pt.Y, pt.Z }).ToList(),
      colors = target.GetColors().Select(o => o.ToArgb()).ToList(),
      bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.GetBoundingBox(true))),
      units = _contextStack.Current.SpeckleUnits
    };
}
