using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class RawPointCloudToSpeckle : IRawConversion<RG.PointCloud, SOG.Pointcloud>
{
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;

  public RawPointCloudToSpeckle(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<RG.Box, SOG.Box> boxConverter
  )
  {
    _contextStack = contextStack;
    _boxConverter = boxConverter;
  }

  public SOG.Pointcloud RawConvert(RG.PointCloud target) =>
    new()
    {
      points = target.GetPoints().SelectMany(pt => new[] { pt.X, pt.Y, pt.Z }).ToList(),
      colors = target.GetColors().Select(o => o.ToArgb()).ToList(),
      bbox = _boxConverter.RawConvert(new RG.Box(target.GetBoundingBox(true))),
      units = _contextStack.Current.SpeckleUnits
    };
}
