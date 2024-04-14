using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolylineToSpeckleConverter
  : IRawConversion<RG.Polyline, SOG.Polyline>,
    IRawConversion<RG.PolylineCurve, SOG.Polyline>
{
  private readonly IRawConversion<RG.Point3d, SOG.Point> _pointConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PolylineToSpeckleConverter(
    IRawConversion<RG.Point3d, SOG.Point> pointConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public SOG.Polyline RawConvert(RG.Polyline target)
  {
    var box = _boxConverter.RawConvert(new RG.Box(target.BoundingBox));
    var points = target.Select(pt => _pointConverter.RawConvert(pt)).ToList();

    if (target.IsClosed)
    {
      points.RemoveAt(points.Count - 1);
    }

    return new SOG.Polyline(
      points.SelectMany(pt => new[] { pt.x, pt.y, pt.z }).ToList(),
      _contextStack.Current.SpeckleUnits
    )
    {
      bbox = box,
      length = target.Length
    };
  }

  public SOG.Polyline RawConvert(RG.PolylineCurve target) => RawConvert(target.ToPolyline());
}
