using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects.BuiltElements.Revit;
using Autodesk.Revit.DB;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Wall), 0)]
public class WallConversionToSpeckle : BaseConversionToSpeckle<DB.Wall, RevitWall>
{
  private readonly IRawConversion<DB.Line, SOG.Line> _lineConverter;

  public WallConversionToSpeckle(IRawConversion<Line, SOG.Line> lineConverter)
  {
    _lineConverter = lineConverter;
  }

  public override RevitWall RawConvert(DB.Wall target)
  {
    RevitWall speckleWall = new();
    speckleWall.family = target.WallType.FamilyName.ToString();
    speckleWall.type = target.WallType.Name;
    speckleWall.baseLine = _lineConverter.RawConvert((Line)((LocationCurve)target.Location).Curve);
    //speckleWall.level = ConvertAndCacheLevel(target, BuiltInParameter.WALL_BASE_CONSTRAINT);
    //speckleWall.topLevel = ConvertAndCacheLevel(target, BuiltInParameter.WALL_HEIGHT_TYPE);
    //speckleWall.height = GetParamValue<double>(target, BuiltInParameter.WALL_USER_HEIGHT_PARAM);
    //speckleWall.baseOffset = GetParamValue<double>(target, BuiltInParameter.WALL_BASE_OFFSET);
    //speckleWall.topOffset = GetParamValue<double>(target, BuiltInParameter.WALL_TOP_OFFSET);
    //speckleWall.structural = GetParamValue<bool>(target, BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT);
    speckleWall.flipped = target.Flipped;

    return speckleWall;
  }
}
