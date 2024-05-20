using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class CircleToSpeckleConverter : ITypedConverter<RG.Circle, SOG.Circle>
{
  private readonly ITypedConverter<RG.Plane, SOG.Plane> _planeConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public CircleToSpeckleConverter(
    ITypedConverter<RG.Plane, SOG.Plane> planeConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _planeConverter = planeConverter;
    _contextStack = contextStack;
  }

  public Base Convert(object target) => RawConvert((RG.Circle)target);

  /// <summary>
  /// Converts a RG.Circle object to a SOG.Circle object.
  /// </summary>
  /// <param name="target">The RG.Circle object to convert.</param>
  /// <returns>The converted SOG.Circle object.</returns>
  /// <remarks>
  /// ⚠️ This conversion assumes the domain of a circle is (0,1) as Rhino Circle types do not have a domain. If you want to preserve the domain use ArcCurve conversion instead.
  /// </remarks>
  public SOG.Circle RawConvert(RG.Circle target) =>
    new(_planeConverter.RawConvert(target.Plane), target.Radius, _contextStack.Current.SpeckleUnits)
    {
      domain = new SOP.Interval(0, 1),
      length = 2 * Math.PI * target.Radius,
      area = Math.PI * Math.Pow(target.Radius, 2)
    };
}
