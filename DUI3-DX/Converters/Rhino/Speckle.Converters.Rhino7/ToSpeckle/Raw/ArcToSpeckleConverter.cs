using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class ArcToSpeckleConverter : ITypedConverter<IRhinoArc, SOG.Arc>
{
  private readonly ITypedConverter<IRhinoPoint3d, SOG.Point> _pointConverter;
  private readonly ITypedConverter<IRhinoPlane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _boxFactory;

  public ArcToSpeckleConverter(
    ITypedConverter<IRhinoPoint3d, SOG.Point> pointConverter,
    ITypedConverter<IRhinoPlane, SOG.Plane> planeConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack, IRhinoBoxFactory boxFactory)
  {
    _pointConverter = pointConverter;
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _boxFactory = boxFactory;
  }

  /// <summary>
  /// Converts a Rhino Arc object to a Speckle Arc object.
  /// </summary>
  /// <param name="target">The Rhino Arc object to convert.</param>
  /// <returns>The converted Speckle Arc object.</returns>
  /// <remarks>
  /// This method assumes the domain of the arc is (0,1) as Arc types in Rhino do not have domain. You may want to request a conversion from ArcCurve instead.
  /// </remarks>
  public SOG.Arc Convert(IRhinoArc target) =>
    new(
      _planeConverter.Convert(target.Plane),
      target.Radius,
      target.StartAngle,
      target.EndAngle,
      target.Angle,
      _contextStack.Current.SpeckleUnits
    )
    {
      startPoint = _pointConverter.Convert(target.StartPoint),
      midPoint = _pointConverter.Convert(target.MidPoint),
      endPoint = _pointConverter.Convert(target.EndPoint),
      domain = new SOP.Interval(0, 1),
      length = target.Length,
      bbox = _boxConverter.Convert(_boxFactory.CreateBox(target.BoundingBox()))
    };
}
