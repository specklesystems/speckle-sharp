using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared;

// POC: There is no validation on this converter to prevent conversion from "not a Revit Beam" to a Speckle Beam.
// This will definitely explode if we tried. Goes back to the `CanConvert` functionality conversation.
public class ColumnConversionToSpeckle : ITypedConverter<IRevitFamilyInstance, SOBR.RevitColumn>
{
  private readonly ITypedConverter<IRevitLocation, Base> _locationConverter;
  private readonly ITypedConverter<IRevitLevel, SOBR.RevitLevel> _levelConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _contextStack;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;

  public ColumnConversionToSpeckle(
    ITypedConverter<IRevitLocation, Base> locationConverter,
    ITypedConverter<IRevitLevel, SOBR.RevitLevel> levelConverter,
    IParameterValueExtractor parameterValueExtractor,
    IDisplayValueExtractor displayValueExtractor,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> contextStack,
    IParameterObjectAssigner parameterObjectAssigner
  )
  {
    _locationConverter = locationConverter;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _contextStack = contextStack;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public SOBR.RevitColumn Convert(IRevitFamilyInstance target)
  {
    var symbol = target.Document.GetElement(target.GetTypeId()).NotNull().ToFamilySymbol().NotNull();

    SOBR.RevitColumn speckleColumn =
      new() { family = symbol.FamilyName, type = target.Document.GetElement(target.GetTypeId()).NotNull().Name };

    if (
      _parameterValueExtractor.TryGetValueAsRevitLevel(
        target,
        RevitBuiltInParameter.FAMILY_BASE_LEVEL_PARAM,
        out var level
      )
    )
    {
      speckleColumn.level = _levelConverter.Convert(level);
    }

    var topLevel = _parameterValueExtractor.GetValueAsRevitLevel(target, RevitBuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

    speckleColumn.topLevel = _levelConverter.Convert(topLevel);
    speckleColumn.baseOffset = _parameterValueExtractor.GetValueAsDouble(
      target,
      RevitBuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM
    );

    speckleColumn.topOffset = _parameterValueExtractor.GetValueAsDouble(
      target,
      RevitBuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM
    );

    speckleColumn.facingFlipped = target.FacingFlipped;
    speckleColumn.handFlipped = target.HandFlipped;
    speckleColumn.isSlanted = target.IsSlantedColumn;

    var locationPoint = target.GetLocationAsLocationPoint();
    if (locationPoint is not null)
    {
      speckleColumn.rotation = locationPoint.Rotation;
    }

    speckleColumn.baseLine =
      GetBaseCurve(target, speckleColumn.topLevel.elevation, speckleColumn.topOffset)
      ?? throw new SpeckleConversionException("Unable to find a valid baseCurve for column");

    speckleColumn.displayValue = _displayValueExtractor.GetDisplayValue(target);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleColumn);

    return speckleColumn;
  }

  private ICurve? GetBaseCurve(IRevitFamilyInstance target, double topLevelElevation, double topLevelOffset)
  {
    Base baseGeometry = _locationConverter.Convert(target.Location);
    ICurve? baseCurve = baseGeometry as ICurve;

    if (baseGeometry is ICurve)
    {
      return baseCurve;
    }
    else if (baseGeometry is SOG.Point basePoint)
    {
      // POC: in existing connector, we are sending column as Revit Instance instead of Column with the following if.
      // I am not sure why. I think this if is checking if the column has a fixed height
      //if (
      //  symbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased
      //  || symbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased
      //)
      //{
      //  return RevitInstanceToSpeckle(revitColumn, out notes, null);
      //}

      return new SOG.Line(
        basePoint,
        new SOG.Point(basePoint.x, basePoint.y, topLevelElevation + topLevelOffset, _contextStack.Current.SpeckleUnits),
        _contextStack.Current.SpeckleUnits
      );
    }

    return null;
  }
}
