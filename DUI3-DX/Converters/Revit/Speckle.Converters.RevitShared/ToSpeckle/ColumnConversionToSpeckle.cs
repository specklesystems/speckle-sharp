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

  public RevitColumn RawConvert(DB.FamilyInstance revitColumn)
  {
    var symbol = revitColumn.Document.GetElement(revitColumn.GetTypeId()) as FamilySymbol;

    var speckleColumn = new RevitColumn();
    speckleColumn.family = symbol.FamilyName;
    speckleColumn.type = revitColumn.Document.GetElement(revitColumn.GetTypeId()).Name;

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      revitColumn,
      BuiltInParameter.FAMILY_BASE_LEVEL_PARAM
    );
    speckleColumn.level = _levelConverter.RawConvert(level);

    var topLevel = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      revitColumn,
      BuiltInParameter.FAMILY_TOP_LEVEL_PARAM
    );
    speckleColumn.topLevel = _levelConverter.RawConvert(topLevel);
    speckleColumn.baseOffset =
      _parameterValueExtractor.GetValueAsDouble(revitColumn, BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM) ?? 0;
    speckleColumn.topOffset =
      _parameterValueExtractor.GetValueAsDouble(revitColumn, BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM) ?? 0;
    speckleColumn.facingFlipped = revitColumn.FacingFlipped;
    speckleColumn.handFlipped = revitColumn.HandFlipped;
    speckleColumn.isSlanted = revitColumn.IsSlantedColumn;
    //speckleColumn.structural = revitColumn.StructuralType == StructuralType.Column;

    //geometry
    var baseGeometry = _locationConverter.ConvertToBase(revitColumn.Location);
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

    _parameterObjectAssigner.AssignParametersToBase(revitColumn, speckleColumn);

    if (revitColumn.Location is LocationPoint locationPoint)
    {
      speckleColumn.rotation = locationPoint.Rotation;
    }

    speckleColumn.displayValue = _displayValueExtractor.GetDisplayValue(revitColumn);

    return speckleColumn;
  }
}
