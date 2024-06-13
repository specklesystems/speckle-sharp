using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Converters.RevitShared.Extensions;
using Objects.BuiltElements.Revit;
using Speckle.Converters.Revit2023.ToSpeckle;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: needs review feels, BIG, feels like it could be broken down..
// i.e. GetParams(), GetGeom()? feels like it's doing too much
[NameAndRankValue(nameof(IRevitWall), 0)]
public class WallTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<IRevitWall, SOBR.RevitWall>
{
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;
  private readonly ITypedConverter<IRevitLevel, SOBR.RevitLevel> _levelConverter;
  private readonly ITypedConverter<IRevitCurveArrArray, List<SOG.Polycurve>> _curveArrArrayConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;
  private readonly IRootToSpeckleConverter _converter;
  private readonly IRevitFilterFactory _revitFilterFactory;

  public WallTopLevelConverterToSpeckle(
    ITypedConverter<IRevitCurve, ICurve> curveConverter,
    ITypedConverter<IRevitLevel, SOBR.RevitLevel> levelConverter,
    ITypedConverter<IRevitCurveArrArray, List<SOG.Polycurve>> curveArrArrayConverter,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    IParameterValueExtractor parameterValueExtractor,
    IDisplayValueExtractor displayValueExtractor,
    IParameterObjectAssigner parameterObjectAssigner,
    IRootToSpeckleConverter converter,
    IRevitFilterFactory revitFilterFactory
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
    _revitFilterFactory = revitFilterFactory;
  }

  public override SOBR.RevitWall Convert(IRevitWall target)
  {
    SOBR.RevitWall speckleWall = new() { family = target.WallType.FamilyName.ToString(), type = target.WallType.Name };

    AssignSpecificParameters(target, speckleWall);
    AssignVoids(target, speckleWall);
    AssignHostedElements(speckleWall, GetChildElements(target));
    AssignDisplayValue(target, speckleWall);
    _parameterObjectAssigner.AssignParametersToBase(target, speckleWall);

    return speckleWall;
  }

  private void AssignSpecificParameters(IRevitWall target, RevitWall speckleWall)
  {
    var locationCurve = target.GetLocationAsLocationCurve();
    if (locationCurve is null)
    {
      throw new SpeckleConversionException(
        "Incorrect assumption was made that all Revit Wall location properties would be of type \"LocationCurve\""
      );
    }

    speckleWall.baseLine = _curveConverter.Convert(locationCurve.Curve);

    var level = _parameterValueExtractor.GetValueAsRevitLevel(target, RevitBuiltInParameter.WALL_BASE_CONSTRAINT);
    speckleWall.level = _levelConverter.Convert(level.NotNull());

    var topLevel = _parameterValueExtractor.GetValueAsRevitLevel(target, RevitBuiltInParameter.WALL_BASE_CONSTRAINT);
    speckleWall.topLevel = _levelConverter.Convert(topLevel.NotNull());

    // POC : what to do if these parameters are unset (instead of assigning default)
    _ = _parameterValueExtractor.TryGetValueAsDouble(
      target,
      RevitBuiltInParameter.WALL_USER_HEIGHT_PARAM,
      out double? height
    );
    speckleWall.height = height ?? 0;
    _ = _parameterValueExtractor.TryGetValueAsDouble(
      target,
      RevitBuiltInParameter.WALL_BASE_OFFSET,
      out double? baseOffset
    );
    speckleWall.baseOffset = baseOffset ?? 0;
    _ = _parameterValueExtractor.TryGetValueAsDouble(
      target,
      RevitBuiltInParameter.WALL_TOP_OFFSET,
      out double? topOffset
    );
    speckleWall.topOffset = topOffset ?? 0;
    speckleWall.structural =
      _parameterValueExtractor.GetValueAsBool(target, RevitBuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT) ?? false;
    speckleWall.flipped = target.Flipped;
  }

  private List<Base> GetChildElements(IRevitWall target)
  {
    List<Base> wallChildren = new();
    var grid = target.CurtainGrid;
    if (grid is not null)
    {
      wallChildren.AddRange(ConvertElements(grid.GetMullionIds()));
      wallChildren.AddRange(ConvertElements(grid.GetPanelIds()));
    }
    else if (target.IsStackedWall)
    {
      wallChildren.AddRange(ConvertElements(target.GetStackedWallMemberIds()));
    }
    wallChildren.AddRange(ConvertElements(target.GetHostedElementIds(_revitFilterFactory)));
    return wallChildren;
  }

  private IEnumerable<Base> ConvertElements(IEnumerable<IRevitElementId> elementIds)
  {
    foreach (IRevitElementId elementId in elementIds)
    {
      yield return _converter.Convert(_contextStack.Current.Document.GetElement(elementId).NotNull());
    }
  }

  private void AssignDisplayValue(IRevitWall target, RevitWall speckleWall)
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

  private void AssignVoids(IRevitWall target, SOBR.RevitWall speckleWall)
  {
    IRevitCurveArrArray? profile = target.Document.GetElement(target.SketchId)?.ToSketch()?.Profile;
    if (profile is null)
    {
      return;
    }

    List<SOG.Polycurve> polycurves = _curveArrArrayConverter.Convert(profile);

    if (polycurves.Count > 1)
    {
      // POC: we have been assuming that the first curve is the element and the rest of the curves are openings
      // this isn't always true
      // https://spockle.atlassian.net/browse/CNX-9396
      speckleWall["voids"] = polycurves.Skip(1).ToList();
    }
  }
}
