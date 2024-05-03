using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.Helpers;

public class SlopeArrowExtractor : ISlopeArrowExtractor
{
  private readonly IRawConversion<DB.XYZ, SOG.Point> _pointConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public SlopeArrowExtractor(
    IRawConversion<DB.XYZ, SOG.Point> pointConverter,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _pointConverter = pointConverter;
    _parameterValueExtractor = parameterValueExtractor;
  }

  public DB.ModelLine? GetSlopeArrow(DB.Element element)
  {
    IList<DB.ElementId>? elementIds = null;
    if (element is DB.Floor floor)
    {
      elementIds = ((DB.Sketch)floor.Document.GetElement(floor.SketchId)).GetAllElements();
    }

    if (elementIds == null)
    {
      using var modelLineFilter = new DB.ElementCategoryFilter(DB.BuiltInCategory.OST_SketchLines);
      elementIds = element.GetDependentElements(modelLineFilter);
    }

    foreach (var elementId in elementIds)
    {
      if (element.Document.GetElement(elementId) is not DB.ModelLine line)
      {
        continue;
      }

      var offsetAtTailParameter = line.get_Parameter(DB.BuiltInParameter.SLOPE_START_HEIGHT);
      if (offsetAtTailParameter != null)
      {
        return line;
      }
    }
    return null;
  }

  public SOG.Point GetSlopeArrowHead(DB.ModelLine slopeArrow)
  {
    return _pointConverter.RawConvert(((DB.LocationCurve)slopeArrow.Location).Curve.GetEndPoint(1));
  }

  public SOG.Point GetSlopeArrowTail(DB.ModelLine slopeArrow)
  {
    return _pointConverter.RawConvert(((DB.LocationCurve)slopeArrow.Location).Curve.GetEndPoint(0));
  }

  public double GetSlopeArrowTailOffset(DB.ModelLine slopeArrow)
  {
    return _parameterValueExtractor.GetValueAsDouble(slopeArrow, DB.BuiltInParameter.SLOPE_START_HEIGHT);
  }

  public double GetSlopeArrowHeadOffset(DB.ModelLine slopeArrow, double tailOffset, out double slope)
  {
    var specifyOffset = _parameterValueExtractor.GetValueAsInt(slopeArrow, DB.BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET);

    var lineLength = _parameterValueExtractor.GetValueAsDouble(slopeArrow, DB.BuiltInParameter.CURVE_ELEM_LENGTH);

    slope = 0;
    double headOffset = 0;
    // 1 corrosponds to the "slope" option
    if (specifyOffset == 1)
    {
      // in this scenario, slope is returned as a percentage. Divide by 100 to get the unitless form
      slope = _parameterValueExtractor.GetValueAsDouble(slopeArrow, DB.BuiltInParameter.ROOF_SLOPE) / 100d;
      headOffset = tailOffset + lineLength * Math.Sin(Math.Atan(slope));
    }
    else if (specifyOffset == 0) // 0 corrospondes to the "height at tail" option
    {
      headOffset = _parameterValueExtractor.GetValueAsDouble(slopeArrow, DB.BuiltInParameter.SLOPE_END_HEIGHT);

      slope = (headOffset - tailOffset) / lineLength;
    }

    return headOffset;
  }
}
