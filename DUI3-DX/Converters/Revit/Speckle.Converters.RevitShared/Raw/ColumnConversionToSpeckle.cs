using Autodesk.Revit.DB;
using Objects;
using Objects.BuiltElements.Revit;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: There is no validation on this converter to prevent conversion from "not a Revit Beam" to a Speckle Beam.
// This will definitely explode if we tried. Goes back to the `CanConvert` functionality conversation.
public class ColumnConversionToSpeckle : ITypedConverter<DB.FamilyInstance, RevitColumn>
{
  private readonly ITypedConverter<Location, Base> _locationConverter;
  private readonly ITypedConverter<Level, RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly IRevitConversionContextStack _contextStack;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public ColumnConversionToSpeckle(
    ITypedConverter<Location, Base> locationConverter,
    ITypedConverter<Level, RevitLevel> levelConverter,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    IRevitConversionContextStack contextStack,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _locationConverter = locationConverter;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _contextStack = contextStack;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public RevitColumn Convert(DB.FamilyInstance target)
  {
    FamilySymbol symbol = (FamilySymbol)target.Document.GetElement(target.GetTypeId());

    RevitColumn speckleColumn =
      new() { family = symbol.FamilyName, type = target.Document.GetElement(target.GetTypeId()).Name };

    Level level = _parameterValueExtractor.GetValueAsDocumentObject<Level>(
      target,
      BuiltInParameter.FAMILY_BASE_LEVEL_PARAM
    );
    speckleColumn.level = _levelConverter.Convert(level);

    Level topLevel = _parameterValueExtractor.GetValueAsDocumentObject<Level>(
      target,
      BuiltInParameter.FAMILY_TOP_LEVEL_PARAM
    );

    speckleColumn.topLevel = _levelConverter.Convert(topLevel);
    speckleColumn.baseOffset = _parameterValueExtractor.GetValueAsDouble(
      target,
      DB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM
    );
    speckleColumn.topOffset = _parameterValueExtractor.GetValueAsDouble(
      target,
      DB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM
    );

    speckleColumn.facingFlipped = target.FacingFlipped;
    speckleColumn.handFlipped = target.HandFlipped;
    speckleColumn.isSlanted = target.IsSlantedColumn;

    if (target.Location is LocationPoint locationPoint)
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

  private ICurve? GetBaseCurve(DB.FamilyInstance target, double topLevelElevation, double topLevelOffset)
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
