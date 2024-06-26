using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class CircleToSpeckleConverter : ITypedConverter<IRhinoCircle, SOG.Circle>
{
  private readonly ITypedConverter<IRhinoPlane, SOG.Plane> _planeConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;

  public CircleToSpeckleConverter(
    ITypedConverter<IRhinoPlane, SOG.Plane> planeConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => Convert((IRhinoCircle)target);

  /// <summary>
  /// Converts a IRhinoCircle object to a SOG.Circle object.
  /// </summary>
  /// <param name="target">The IRhinoCircle object to convert.</param>
  /// <returns>The converted SOG.Circle object.</returns>
  /// <remarks>
  /// ⚠️ This conversion assumes the domain of a circle is (0,1) as Rhino Circle types do not have a domain. If you want to preserve the domain use ArcCurve conversion instead.
  /// </remarks>
  public SOG.Circle Convert(IRhinoCircle target) =>
    new(_planeConverter.Convert(target.Plane), target.Radius, _contextStack.Current.SpeckleUnits)
    {
      domain = new SOP.Interval(0, 1),
      length = 2 * Math.PI * target.Radius,
      area = Math.PI * Math.Pow(target.Radius, 2)
    };
}
