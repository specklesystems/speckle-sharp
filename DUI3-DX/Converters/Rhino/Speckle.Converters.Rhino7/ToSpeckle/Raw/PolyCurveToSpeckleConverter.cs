using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolyCurveToSpeckleConverter : ITypedConverter<IRhinoPolyCurve, SOG.Polycurve>
{
  public ITypedConverter<IRhinoCurve, ICurve>? CurveConverter { get; set; } // POC: CNX-9279 This created a circular dependency on the constructor, making it a property allows for the container to resolve it correctly
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public PolyCurveToSpeckleConverter(
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack, IRhinoBoxFactory rhinoBoxFactory)
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
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
  public SOG.Polycurve Convert(IRhinoPolyCurve target)
  {
    var myPoly = new SOG.Polycurve
    {
      closed = target.IsClosed,
      domain = _intervalConverter.Convert(target.Domain),
      length = target.GetLength(),
      bbox = _boxConverter.Convert(_rhinoBoxFactory.CreateBox(target.GetBoundingBox(true))),
      segments = target.DuplicateSegments().Select(x => CurveConverter.NotNull().Convert(x)).ToList(),
      units = _contextStack.Current.SpeckleUnits
    };
    return myPoly;
  }
}
