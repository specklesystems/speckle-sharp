using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolylineToSpeckleConverter
  : ITypedConverter<IRhinoPolyline, SOG.Polyline>,
    ITypedConverter<IRhinoPolylineCurve, SOG.Polyline>
{
  private readonly ITypedConverter<IRhinoPoint3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public PolylineToSpeckleConverter(
    ITypedConverter<IRhinoPoint3d, SOG.Point> pointConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter, IRhinoBoxFactory rhinoBoxFactory)
  {
    _pointConverter = pointConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _intervalConverter = intervalConverter;
    _rhinoBoxFactory = rhinoBoxFactory;
  }

  /// <summary>
  /// Converts the given Rhino polyline to a Speckle polyline.
  /// </summary>
  /// <param name="target">The Rhino polyline to be converted.</param>
  /// <returns>The converted Speckle polyline.</returns>
  /// <remarks>⚠️ This conversion assumes domain interval is (0,LENGTH) as Rhino Polylines have no domain. If needed, you may want to use PolylineCurve conversion instead. </remarks>
  public SOG.Polyline Convert(IRhinoPolyline target)
  {
    var box = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.BoundingBox));
    var points = target.Select(pt => _pointConverter.Convert(pt)).ToList();

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
      length = target.Count,
      domain = new(0, target.Count),
      closed = target.IsClosed
    };
  }

  /// <summary>
  /// Converts the given Rhino PolylineCurve to a Speckle polyline.
  /// </summary>
  /// <param name="target">The Rhino PolylineCurve to be converted.</param>
  /// <returns>The converted Speckle polyline.</returns>
  /// <remarks>✅ This conversion respects the domain of the original PolylineCurve</remarks>
  public SOG.Polyline Convert(IRhinoPolylineCurve target)
  {
    var result = Convert(target.ToPolyline());
    result.domain = _intervalConverter.Convert(target.Domain);
    return result;
  }
}
