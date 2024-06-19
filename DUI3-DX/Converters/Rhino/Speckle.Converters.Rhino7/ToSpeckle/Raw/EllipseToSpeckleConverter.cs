using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class EllipseToSpeckleConverter : ITypedConverter<IRhinoEllipse, SOG.Ellipse>
{
  private readonly ITypedConverter<IRhinoPlane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public EllipseToSpeckleConverter(
    ITypedConverter<IRhinoPlane, SOG.Plane> planeConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack, IRhinoBoxFactory rhinoBoxFactory)
  {
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
  }

  /// <summary>
  /// Converts a Rhino Ellipse to a Speckle Ellipse.
  /// </summary>
  /// <param name="target">The Rhino Ellipse to convert.</param>
  /// <returns>The converted Speckle Ellipse.</returns>
  /// <remarks>
  /// ⚠️ Rhino ellipses are not curves. The result is a mathematical representation of an ellipse that can be converted into NURBS for display.
  /// </remarks>
  public SOG.Ellipse Convert(IRhinoEllipse target)
  {
    var nurbsCurve = target.ToNurbsCurve();
    return new(
      _planeConverter.Convert(target.Plane),
      target.Radius1,
      target.Radius2,
      _contextStack.Current.SpeckleUnits
    )
    {
      domain = new SOP.Interval(0, 1),
      length = nurbsCurve.GetLength(),
      area = Math.PI * target.Radius1 * target.Radius2,
      bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(nurbsCurve.GetBoundingBox(true)))
    };
  }
}
