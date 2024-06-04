namespace Speckle.Converters.RevitShared.Helpers;

public interface ISlopeArrowExtractor
{
  DB.ModelLine? GetSlopeArrow(DB.Element element);
  SOG.Point GetSlopeArrowHead(DB.ModelLine slopeArrow);
  SOG.Point GetSlopeArrowTail(DB.ModelLine slopeArrow);
  double GetSlopeArrowTailOffset(DB.ModelLine slopeArrow);
  double GetSlopeArrowHeadOffset(DB.ModelLine slopeArrow, double tailOffset, out double slope);
}
