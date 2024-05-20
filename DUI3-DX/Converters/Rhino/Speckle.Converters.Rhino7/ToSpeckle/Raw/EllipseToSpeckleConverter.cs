using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class EllipseToSpeckleConverter : ITypedConverter<RG.Ellipse, SOG.Ellipse>
{
  private readonly ITypedConverter<RG.Plane, SOG.Plane> _planeConverter;
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public EllipseToSpeckleConverter(
    ITypedConverter<RG.Plane, SOG.Plane> planeConverter,
    ITypedConverter<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino Ellipse to a Speckle Ellipse.
  /// </summary>
  /// <param name="target">The Rhino Ellipse to convert.</param>
  /// <returns>The converted Speckle Ellipse.</returns>
  /// <remarks>
  /// ⚠️ Rhino ellipses are not curves. The result is a mathematical representation of an ellipse that can be converted into NURBS for display.
  /// </remarks>
  public SOG.Ellipse Convert(RG.Ellipse target)
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
      bbox = _boxConverter.Convert(new RG.Box(nurbsCurve.GetBoundingBox(true)))
    };
  }
}
