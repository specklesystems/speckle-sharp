using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Converters.RevitShared.Extensions;
using Objects.BuiltElements.Revit;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: needs review feels, BIG, feels like it could be broken down..
// i.e. GetParams(), GetGeom()? feels like it's doing too much
[NameAndRankValue(nameof(DB.Wall), 0)]
public class WallConversionToSpeckle : BaseConversionToSpeckle<DB.Wall, SOBR.RevitWall>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.CurveArrArray, List<SOG.Polycurve>> _curveArrArrayConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly RevitConversionContextStack _contextStack;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;
  private readonly ISpeckleConverterToSpeckle _converter;

  public WallConversionToSpeckle(
    IRawConversion<DB.Curve, ICurve> curveConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    IRawConversion<DB.CurveArrArray, List<SOG.Polycurve>> curveArrArrayConverter,
    RevitConversionContextStack contextStack,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    ParameterObjectAssigner parameterObjectAssigner,
    ISpeckleConverterToSpeckle converter
  )
  {
    _curveConverter = curveConverter;
    _levelConverter = levelConverter;
    _curveArrArrayConverter = curveArrArrayConverter;
    _contextStack = contextStack;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
    _converter = converter;
  }

  public override SOBR.RevitWall RawConvert(DB.Wall target)
  {
    SOBR.RevitWall speckleWall = new() { family = target.WallType.FamilyName.ToString(), type = target.WallType.Name };

    AssignSpecificParameters(target, speckleWall);
    AssignVoids(target, speckleWall);
    AssignHostedElements(speckleWall, GetChildElements(target));
    AssignDisplayValue(target, speckleWall);
    _parameterObjectAssigner.AssignParametersToBase(target, speckleWall);

    return speckleWall;
  }

  private void AssignSpecificParameters(DB.Wall target, RevitWall speckleWall)
  {
    if (target.Location is not DB.LocationCurve locationCurve)
    {
      throw new SpeckleConversionException(
        "Incorrect assumption was made that all Revit Wall location properties would be of type \"LocationCurve\""
      );
    }

    speckleWall.baseLine = _curveConverter.RawConvert(locationCurve.Curve);

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.WALL_BASE_CONSTRAINT
    );
    speckleWall.level = _levelConverter.RawConvert(level);

    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.WALL_BASE_CONSTRAINT
    );
    speckleWall.topLevel = _levelConverter.RawConvert(topLevel);

    // POC : what to do if these parameters are unset (instead of assigning default)
    _ = _parameterValueExtractor.TryGetValueAsDouble(
      target,
      DB.BuiltInParameter.WALL_USER_HEIGHT_PARAM,
      out double? height
    );
    speckleWall.height = height ?? 0;
    _ = _parameterValueExtractor.TryGetValueAsDouble(
      target,
      DB.BuiltInParameter.WALL_BASE_OFFSET,
      out double? baseOffset
    );
    speckleWall.baseOffset = baseOffset ?? 0;
    _ = _parameterValueExtractor.TryGetValueAsDouble(
      target,
      DB.BuiltInParameter.WALL_TOP_OFFSET,
      out double? topOffset
    );
    speckleWall.topOffset = topOffset ?? 0;
    speckleWall.structural =
      _parameterValueExtractor.GetValueAsBool(target, DB.BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT) ?? false;
    speckleWall.flipped = target.Flipped;
  }

  private List<Base> GetChildElements(DB.Wall target)
  {
    List<Base> wallChildren = new();
    if (target.CurtainGrid is DB.CurtainGrid grid)
    {
      wallChildren.AddRange(ConvertElements(grid.GetMullionIds()));
      wallChildren.AddRange(ConvertElements(grid.GetPanelIds()));
    }
    else if (target.IsStackedWall)
    {
      wallChildren.AddRange(ConvertElements(target.GetStackedWallMemberIds()));
    }
    wallChildren.AddRange(ConvertElements(target.GetHostedElementIds()));
    return wallChildren;
  }

  private IEnumerable<Base> ConvertElements(IEnumerable<DB.ElementId> elementIds)
  {
    foreach (DB.ElementId elementId in elementIds)
    {
      yield return _converter.Convert(_contextStack.Current.Document.Document.GetElement(elementId));
    }
  }

  private void AssignDisplayValue(DB.Wall target, RevitWall speckleWall)
  {
    if (target.CurtainGrid is null)
    {
      speckleWall.displayValue = _displayValueExtractor.GetDisplayValue(target);
    }
    else
    {
      // POC: I have no why previously we were setting the display value, and then unsetting it.
      // Probably curtain walls need a special case/etc.?
      speckleWall.displayValue = new List<SOG.Mesh>();
    }
  }

  private void AssignHostedElements(SOBR.RevitWall speckleWall, List<Base> hostedObjects)
  {
    if (hostedObjects.Count == 0)
    {
      return;
    }

    if (speckleWall.GetDetachedProp("elements") is List<Base> elements)
    {
      elements.AddRange(hostedObjects);
    }
    else
    {
      speckleWall.SetDetachedProp("elements", hostedObjects);
    }
  }

  private void AssignVoids(DB.Wall target, SOBR.RevitWall speckleWall)
  {
    DB.CurveArrArray? profile = ((DB.Sketch)target.Document.GetElement(target.SketchId))?.Profile;
    if (profile is null)
    {
      return;
    }

    List<SOG.Polycurve> polycurves = _curveArrArrayConverter.RawConvert(profile);

    if (polycurves.Count > 1)
    {
      // POC: we have been assuming that the first curve is the element and the rest of the curves are openings
      // this isn't always true
      // https://spockle.atlassian.net/browse/CNX-9396
      speckleWall["voids"] = polycurves.Skip(1).ToList();
    }
  }
}
