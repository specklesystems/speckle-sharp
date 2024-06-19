using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Rhino7.Interfaces;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolyCurveToSpeckleConverter : ITypedConverter<IRhinoPolyCurve, SOG.Polycurve>
{
  private readonly ITypedConverter<IRhinoCurve, ICurve> _curveConverter;
  private readonly ITypedConverter<IRhinoInterval, SOP.Interval> _intervalConverter;
  private readonly ITypedConverter<IRhinoBox, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<IRhinoDoc, RhinoUnitSystem> _contextStack;
  private readonly IRhinoBoxFactory _rhinoBoxFactory;

  public PolyCurveToSpeckleConverter(
    ITypedConverter<IRhinoInterval, SOP.Interval> intervalConverter,
    ITypedConverter<IRhinoBox, SOG.Box> boxConverter,
    IConversionContextStack<IRhinoDoc, RhinoUnitSystem> contextStack,
    IRhinoBoxFactory rhinoBoxFactory,
    ITypedConverter<IRhinoCurve, ICurve> curveConverter
  )
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
    _rhinoBoxFactory = rhinoBoxFactory;
    _curveConverter = curveConverter;
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
      segments = target.DuplicateSegments().Select(x => _curveConverter.Convert(x)).ToList(),
      units = _contextStack.Current.SpeckleUnits
    };
    return myPoly;
  }
}
