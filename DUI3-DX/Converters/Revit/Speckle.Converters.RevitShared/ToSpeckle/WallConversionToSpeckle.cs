using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;
using Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Wall), 0)]
public class WallConversionToSpeckle : BaseConversionToSpeckle<DB.Wall, RevitWall>
{
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.Level, RevitLevel> _levelConverter;
  private readonly ParameterConversionToSpeckle _paramConverter;
  private readonly RevitConversionContextStack _contextStack;

  public WallConversionToSpeckle(
    IRawConversion<Curve, ICurve> curveConverter,
    IRawConversion<Level, RevitLevel> levelConverter,
    ParameterConversionToSpeckle paramConverter,
    RevitConversionContextStack contextStack
  )
  {
    _curveConverter = curveConverter;
    _levelConverter = levelConverter;
    _paramConverter = paramConverter;
    _contextStack = contextStack;
  }

  public override RevitWall RawConvert(DB.Wall target)
  {
    RevitWall speckleWall = new() { family = target.WallType.FamilyName.ToString(), type = target.WallType.Name };

    if (target.Location is not LocationCurve locationCurve)
    {
      throw new SpeckleConversionException(
        "Incorrect assumption was made that all Revit Wall location properties would be of type \"LocationCurve\""
      );
    }

    speckleWall.baseLine = _curveConverter.RawConvert(locationCurve.Curve);

    var levelElementId = _paramConverter.GetValueAsElementId(target, BuiltInParameter.WALL_BASE_CONSTRAINT);
    var level = _contextStack.Current.Document.Document.GetElement(levelElementId) as DB.Level;
    speckleWall.level = _levelConverter.RawConvert(level);

    var topLevelElementId = _paramConverter.GetValueAsElementId(target, BuiltInParameter.WALL_BASE_CONSTRAINT);
    var topLevel = _contextStack.Current.Document.Document.GetElement(topLevelElementId) as DB.Level;
    speckleWall.topLevel = _levelConverter.RawConvert(topLevel);

    // POC : what to do if these parameters are unset (instead of assigning 0)
    speckleWall.height = _paramConverter.GetValueAsDouble(target, BuiltInParameter.WALL_USER_HEIGHT_PARAM) ?? 0;
    speckleWall.baseOffset = _paramConverter.GetValueAsDouble(target, BuiltInParameter.WALL_BASE_OFFSET) ?? 0;
    speckleWall.topOffset = _paramConverter.GetValueAsDouble(target, BuiltInParameter.WALL_TOP_OFFSET) ?? 0;
    speckleWall.structural =
      _paramConverter.GetValueAsBool(target, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT) ?? false;
    speckleWall.flipped = target.Flipped;

    return speckleWall;
  }
}
