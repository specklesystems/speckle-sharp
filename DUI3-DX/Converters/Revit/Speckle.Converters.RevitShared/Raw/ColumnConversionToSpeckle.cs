using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Core.Models;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: There is no validation on this converter to prevent conversion from "not a Revit Beam" to a Speckle Beam.
// This will definitely explode if we tried. Goes back to the `CanConvert` functionality conversation.
public class ColumnConversionToSpeckle : IRawConversion<DB.FamilyInstance, SOBR.RevitColumn>
{
  private readonly IRawConversion<DB.Location, Base> _locationConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly RevitConversionContextStack _contextStack;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  // POC: could be broken down more granular, maybe injected, maybe even if methods
  // GetParameters()
  // GetGeometry()
  // etc...
  public ColumnConversionToSpeckle(
    IRawConversion<DB.Location, Base> locationConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    RevitConversionContextStack contextStack,
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

  public SOBR.RevitColumn RawConvert(DB.FamilyInstance target)
  {
    var symbol = (DB.FamilySymbol)target.Document.GetElement(target.GetTypeId());

    var speckleColumn = new SOBR.RevitColumn
    {
      family = symbol.FamilyName,
      type = target.Document.GetElement(target.GetTypeId()).Name
    };

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.FAMILY_BASE_LEVEL_PARAM
    );
    speckleColumn.level = _levelConverter.RawConvert(level);

    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.FAMILY_TOP_LEVEL_PARAM
    );
    speckleColumn.topLevel = _levelConverter.RawConvert(topLevel);
    speckleColumn.baseOffset =
      _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM) ?? 0;
    speckleColumn.topOffset =
      _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM) ?? 0;
    speckleColumn.facingFlipped = target.FacingFlipped;
    speckleColumn.handFlipped = target.HandFlipped;
    speckleColumn.isSlanted = target.IsSlantedColumn;
    //speckleColumn.structural = revitColumn.StructuralType == StructuralType.Column;

    //geometry
    var baseGeometry = _locationConverter.RawConvert(target.Location);
    var baseLine = baseGeometry as ICurve;

    //make line from point and height
    if (baseLine == null && baseGeometry is SOG.Point basePoint)
    {
      if (
        symbol.Family.FamilyPlacementType == DB.FamilyPlacementType.OneLevelBased
        || symbol.Family.FamilyPlacementType == DB.FamilyPlacementType.WorkPlaneBased
      )
      {
        //return RevitInstanceToSpeckle(revitColumn, out notes, null);
        throw new SpeckleConversionException();
      }

      var elevation = speckleColumn.topLevel.elevation;
      baseLine = new SOG.Line(
        basePoint,
        new SOG.Point(
          basePoint.x,
          basePoint.y,
          elevation + speckleColumn.topOffset,
          _contextStack.Current.SpeckleUnits
        ),
        _contextStack.Current.SpeckleUnits
      );
    }

    if (baseLine == null)
    {
      // return RevitElementToSpeckle(revitColumn, out notes);
      throw new SpeckleConversionException();
    }

    speckleColumn.baseLine = baseLine; //all speckle columns should be line based

    _parameterObjectAssigner.AssignParametersToBase(target, speckleColumn);

    if (target.Location is DB.LocationPoint locationPoint)
    {
      speckleColumn.rotation = locationPoint.Rotation;
    }

    speckleColumn.displayValue = _displayValueExtractor.GetDisplayValue(target);

    return speckleColumn;
  }
}
