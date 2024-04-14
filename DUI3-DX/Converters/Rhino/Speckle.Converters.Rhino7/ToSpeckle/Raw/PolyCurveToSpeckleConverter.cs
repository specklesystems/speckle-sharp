using Objects;
using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToSpeckle.Raw;

public class PolyCurveToSpeckleConverter : IRawConversion<RG.PolyCurve, SOG.Polycurve>
{
  public IRawConversion<RG.Curve, ICurve>? CurveConverter { get; set; } // POC: This created a circular dependency on the constructor, making it a property allows for the container to resolve it correctly
  private readonly IRawConversion<RG.Interval, SOP.Interval> _intervalConverter;
  private readonly IRawConversion<RG.Box, SOG.Box> _boxConverter;
  private readonly IConversionContextStack<RhinoDoc, UnitSystem> _contextStack;

  public PolyCurveToSpeckleConverter(
    IRawConversion<RG.Interval, SOP.Interval> intervalConverter,
    IRawConversion<RG.Box, SOG.Box> boxConverter,
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack
  )
  {
    _intervalConverter = intervalConverter;
    _boxConverter = boxConverter;
    _contextStack = contextStack;
  }

  public SOG.Polycurve RawConvert(RG.PolyCurve target)
  {
    var removeNestingSuccess = target.RemoveNesting();
    if (!removeNestingSuccess)
    {
      /*
       * POC: Potentially log as a warning when logger is added.
       * Failing to remove nesting could mean something could not be right, but it doesn't mean we didn't manage to convert a valid PolyCurve.
       */
    }

    var myPoly = new SOG.Polycurve
    {
      closed = target.IsClosed,
      domain = _intervalConverter.RawConvert(target.Domain),
      length = target.GetLength(),
      bbox = _boxConverter.RawConvert(new RG.Box(target.GetBoundingBox(true))),
      segments = target.DuplicateSegments().Select(CurveConverter!.RawConvert).ToList(),
      units = _contextStack.Current.SpeckleUnits
    };
    return myPoly;
  }
}
