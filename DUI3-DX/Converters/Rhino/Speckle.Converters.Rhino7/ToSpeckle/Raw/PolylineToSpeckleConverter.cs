using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolylineToSpeckleConverter
  : ITypedConverter<RG.Polyline, SOG.Polyline>,
    ITypedConverter<RG.PolylineCurve, SOG.Polyline>
{
  private readonly ITypedConverter<RG.Point3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;
  private readonly ITypedConverter<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PolylineToSpeckleConverter(
    ITypedConverter<RG.Point3d, SOG.Point> pointConverter,
    ITypedConverter<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    ITypedConverter<RG.Interval, SOP.Interval> intervalConverter
  )
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _intervalConverter = intervalConverter;
  }

  /// <summary>
  /// Converts the given Rhino polyline to a Speckle polyline.
  /// </summary>
  /// <param name="target">The Rhino polyline to be converted.</param>
  /// <returns>The converted Speckle polyline.</returns>
  /// <remarks>⚠️ This conversion assumes domain interval is (0,LENGTH) as Rhino Polylines have no domain. If needed, you may want to use PolylineCurve conversion instead. </remarks>
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
      length = target.Length,
      domain = new(0, target.Length),
      closed = target.IsClosed
    };
  }

  /// <summary>
  /// Converts the given Rhino PolylineCurve to a Speckle polyline.
  /// </summary>
  /// <param name="target">The Rhino PolylineCurve to be converted.</param>
  /// <returns>The converted Speckle polyline.</returns>
  /// <remarks>✅ This conversion respects the domain of the original PolylineCurve</remarks>
  public SOG.Polyline RawConvert(RG.PolylineCurve target)
  {
    var result = RawConvert(target.ToPolyline());
    result.domain = _intervalConverter.RawConvert(target.Domain);
    return result;
  }
}
