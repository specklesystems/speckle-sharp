using Autodesk.Revit.DB;
using Objects;
using Objects.BuiltElements.Revit;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class ColumnConversionToSpeckle : IRawConversion<DB.FamilyInstance, SOBR.RevitColumn>
{
  private readonly IRawConversion<DB.Location> _locationConverter;
  private readonly IRawConversion<DB.Level, RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly RevitConversionContextStack _contextStack;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  // POC: could be broken down more granular, maybe injected, maybe even if methods
  // GetParameters()
  // GetGeometry()
  // etc...
  public ColumnConversionToSpeckle(
    IRawConversion<Location> locationConverter,
    IRawConversion<Level, RevitLevel> levelConverter,
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

  public RevitColumn RawConvert(DB.FamilyInstance target)
  {
    var symbol = (FamilySymbol)target.Document.GetElement(target.GetTypeId());

    var speckleColumn = new RevitColumn
    {
      family = symbol.FamilyName,
      type = target.Document.GetElement(target.GetTypeId()).Name
    };

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      BuiltInParameter.FAMILY_BASE_LEVEL_PARAM
    );
    speckleColumn.level = _levelConverter.RawConvert(level);

    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      BuiltInParameter.FAMILY_TOP_LEVEL_PARAM
    );
    speckleColumn.topLevel = _levelConverter.RawConvert(topLevel);
    speckleColumn.baseOffset =
      _parameterValueExtractor.GetValueAsDouble(target, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM) ?? 0;
    speckleColumn.topOffset =
      _parameterValueExtractor.GetValueAsDouble(target, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM) ?? 0;
    speckleColumn.facingFlipped = target.FacingFlipped;
    speckleColumn.handFlipped = target.HandFlipped;
    speckleColumn.isSlanted = target.IsSlantedColumn;
    //speckleColumn.structural = revitColumn.StructuralType == StructuralType.Column;

    //geometry
    var baseGeometry = _locationConverter.ConvertToBase(target.Location);
    var baseLine = baseGeometry as ICurve;

    //make line from point and height
    if (baseLine == null && baseGeometry is Point basePoint)
    {
      if (
        symbol.Family.FamilyPlacementType == FamilyPlacementType.OneLevelBased
        || symbol.Family.FamilyPlacementType == FamilyPlacementType.WorkPlaneBased
      )
      {
        //return RevitInstanceToSpeckle(revitColumn, out notes, null);
        throw new SpeckleConversionException();
      }

      var elevation = speckleColumn.topLevel.elevation;
      baseLine = new Line(
        basePoint,
        new Point(basePoint.x, basePoint.y, elevation + speckleColumn.topOffset, _contextStack.Current.SpeckleUnits),
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

    if (target.Location is LocationPoint locationPoint)
    {
      speckleColumn.rotation = locationPoint.Rotation;
    }

    speckleColumn.displayValue = _displayValueExtractor.GetDisplayValue(target);

    return speckleColumn;
  }
}
