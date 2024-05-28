using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolyCurveToSpeckleConverter : ITypedConverter<RG.PolyCurve, SOG.Polycurve>
{
  public ITypedConverter<RG.Curve, ICurve>? CurveConverter { get; set; } // POC: CNX-9279 This created a circular dependency on the constructor, making it a property allows for the container to resolve it correctly
  private readonly ITypedConverter<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly ITypedConverter<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PolyCurveToSpeckleConverter(
    ITypedConverter<RG.Interval, SOP.Interval> intervalConverter,
    ITypedConverter<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  /// <summary>
  /// Converts a Rhino PolyCurve to a Speckle Polycurve.
  /// </summary>
  /// <param name="target">The Rhino PolyCurve to convert.</param>
  /// <returns>The converted Speckle Polycurve.</returns>
  /// <remarks>
  /// This method removes the nesting of the PolyCurve by duplicating the segments at a granular level.
  /// All PolyLIne, PolyCurve and NURBS curves with G1 discontinuities will be broken down.
  /// </remarks>
  public SOG.Polycurve Convert(RG.PolyCurve target)
  {
    var myPoly = new SOG.Polycurve
    {
      closed = target.IsClosed,
      domain = _intervalConverter.Convert(target.Domain),
      length = target.GetLength(),
      bbox = _boxConverter.Convert(new RG.Box(target.GetBoundingBox(true))),
      segments = target.DuplicateSegments().Select(x => CurveConverter.NotNull().Convert(x)).ToList(),
      units = _contextStack.Current.SpeckleUnits
    };
    return myPoly;
  }
}
