using Objects;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Core.Models;

namespace Speckle.Converters.Common;

// POC: reminder - writing classes and creating interfaces is a bit like organising your space
// if you have a structure for organising things, your interfaces, then finding your stuff, your classes & methods, becomes easy
// having a lack of interfaces or large interfaces is a bit like lacking structure, when all of your stuff, your classes & methods
// clould be anywhere or all in once place - rooting through box 274 for something you need, when said box has a miriad different
// and unrelated items, is no fun. Plus when you need that item, you end up bringing out the whole box/
[NameAndRankValue(nameof(DB.Floor), 0)]
public class FloorConversionToSpeckle : BaseConversionToSpeckle<DB.Floor, SOBR.RevitFloor>
{
  private readonly IRawConversion<DB.CurveArrArray, List<SOG.Polycurve>> _curveArrArrayConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly ISlopeArrowExtractor _slopeArrowExtractor;

  public FloorConversionToSpeckle(
    IRawConversion<DB.CurveArrArray, List<SOG.Polycurve>> curveArrArrayConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    ParameterValueExtractor parameterValueExtractor,
    ParameterObjectAssigner parameterObjectAssigner,
    DisplayValueExtractor displayValueExtractor,
    ISlopeArrowExtractor slopeArrowExtractor
  )
  {
    _curveArrArrayConverter = curveArrArrayConverter;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
    _displayValueExtractor = displayValueExtractor;
    _slopeArrowExtractor = slopeArrowExtractor;
  }

  public override SOBR.RevitFloor RawConvert(DB.Floor target)
  {
    SOBR.RevitFloor speckleFloor = new();

    var sketch = (DB.Sketch)target.Document.GetElement(target.SketchId);
    List<SOG.Polycurve> profiles = _curveArrArrayConverter.RawConvert(sketch.Profile);

    DB.ElementType type = (DB.ElementType)target.Document.GetElement(target.GetTypeId());

    speckleFloor.family = type.FamilyName;
    speckleFloor.type = type.Name;

    // POC: Re-evaluate Wall sketch curve extraction, assumption of only one outline is wrong. https://spockle.atlassian.net/browse/CNX-9396
    if (profiles.Count > 0)
    {
      speckleFloor.outline = profiles[0];
    }

    if (profiles.Count > 1)
    {
      speckleFloor.voids = profiles.Skip(1).ToList<ICurve>();
    }

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(target, DB.BuiltInParameter.LEVEL_PARAM);
    speckleFloor.level = _levelConverter.RawConvert(level);
    speckleFloor.structural =
      _parameterValueExtractor.GetValueAsBool(target, DB.BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL) ?? false;

    double? slopeParam = null;
    if (_parameterValueExtractor.TryGetValueAsDouble(target, DB.BuiltInParameter.ROOF_SLOPE, out var slope))
    {
      // Divide by 100 to convert from percentage to unitless ratio (rise over run)
      slopeParam = slope / 100d;
    }

    _parameterObjectAssigner.AssignParametersToBase(target, speckleFloor);
    TryAssignSlopeFromSlopeArrow(target, speckleFloor, slopeParam);

    speckleFloor.displayValue = _displayValueExtractor.GetDisplayValue(target);
    // POC: hosted elements OOS for alpha, but this exists in existing connector
    //_hostedElementConverter.AssignHostedElements(target, speckleCeiling);

    return speckleFloor;
  }

  private void TryAssignSlopeFromSlopeArrow(DB.Floor target, SOBR.RevitFloor speckleFloor, double? slopeParam)
  {
    if (_slopeArrowExtractor.GetSlopeArrow(target) is not DB.ModelLine slopeArrow)
    {
      return;
    }

    var tail = _slopeArrowExtractor.GetSlopeArrowTail(slopeArrow);
    var head = _slopeArrowExtractor.GetSlopeArrowHead(slopeArrow);
    var tailOffset = _slopeArrowExtractor.GetSlopeArrowTailOffset(slopeArrow);
    _ = _slopeArrowExtractor.GetSlopeArrowHeadOffset(slopeArrow, tailOffset, out var slope);

    slopeParam ??= slope;
    speckleFloor.slope = (double)slopeParam;

    speckleFloor.slopeDirection = new SOG.Line(tail, head);
    if (
      speckleFloor["parameters"] is Base parameters
      && parameters["FLOOR_HEIGHTABOVELEVEL_PARAM"] is SOBR.Parameter offsetParam
      && offsetParam.value is double offset
    )
    {
      offsetParam.value = offset + tailOffset;
    }
  }
}
