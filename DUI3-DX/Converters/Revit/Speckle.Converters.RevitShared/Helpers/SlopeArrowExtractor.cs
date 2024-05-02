using Autodesk.Revit.DB;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.Helpers;

public class SlopeArrowExtractor
{
  private readonly IRawConversion<DB.XYZ, SOG.Point> _pointConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;

  public SlopeArrowExtractor(
    IRawConversion<XYZ, SOG.Point> pointConverter,
    ParameterValueExtractor parameterValueExtractor
  )
  {
    _pointConverter = pointConverter;
    _parameterValueExtractor = parameterValueExtractor;
  }

  public ModelLine? GetSlopeArrow(Element element)
  {
    IList<ElementId>? elementIds = null;
    if (element is DB.Floor floor)
    {
      elementIds = ((Sketch)floor.Document.GetElement(floor.SketchId)).GetAllElements();
    }

    if (elementIds == null)
    {
      using var modelLineFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
      elementIds = element.GetDependentElements(modelLineFilter);
    }

    foreach (var elementId in elementIds)
    {
      if (element.Document.GetElement(elementId) is not ModelLine line)
      {
        continue;
      }

      var offsetAtTailParameter = line.get_Parameter(BuiltInParameter.SLOPE_START_HEIGHT);
      if (offsetAtTailParameter != null)
      {
        return line;
      }
    }
    return null;
  }

  public SOG.Point GetSlopeArrowHead(ModelLine slopeArrow)
  {
    return _pointConverter.RawConvert(((LocationCurve)slopeArrow.Location).Curve.GetEndPoint(1));
  }

  public SOG.Point GetSlopeArrowTail(ModelLine slopeArrow)
  {
    return _pointConverter.RawConvert(((LocationCurve)slopeArrow.Location).Curve.GetEndPoint(0));
  }

  public double GetSlopeArrowTailOffset(ModelLine slopeArrow)
  {
    return _parameterValueExtractor.GetValueAsDouble(slopeArrow, BuiltInParameter.SLOPE_START_HEIGHT)
      ?? throw new SpeckleConversionException(
        $"Unexpected null value for slope arrow property {nameof(BuiltInParameter.SLOPE_START_HEIGHT)}"
      );
  }

  public double GetSlopeArrowHeadOffset(ModelLine slopeArrow, double tailOffset, out double slope)
  {
    var specifyOffset =
      _parameterValueExtractor.GetValueAsInt(slopeArrow, BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET)
      ?? throw new SpeckleConversionException(
        $"Unexpected null value for slope arrow property {nameof(BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET)}"
      );

    var lineLength =
      _parameterValueExtractor.GetValueAsDouble(slopeArrow, BuiltInParameter.CURVE_ELEM_LENGTH)
      ?? throw new SpeckleConversionException(
        $"Unexpected null value for slope arrow property {nameof(BuiltInParameter.CURVE_ELEM_LENGTH)}"
      );

    slope = 0;
    double headOffset = 0;
    // 1 corrosponds to the "slope" option
    if (specifyOffset == 1)
    {
      // in this scenario, slope is returned as a percentage. Divide by 100 to get the unitless form
      slope =
        _parameterValueExtractor.GetValueAsDouble(slopeArrow, BuiltInParameter.ROOF_SLOPE) / 100d
        ?? throw new SpeckleConversionException(
          $"Unexpected null value for slope arrow property {nameof(BuiltInParameter.ROOF_SLOPE)}"
        );
      headOffset = tailOffset + lineLength * Math.Sin(Math.Atan(slope));
    }
    else if (specifyOffset == 0) // 0 corrospondes to the "height at tail" option
    {
      headOffset =
        _parameterValueExtractor.GetValueAsDouble(slopeArrow, BuiltInParameter.SLOPE_END_HEIGHT)
        ?? throw new SpeckleConversionException(
          $"Unexpected null value for slope arrow property {nameof(BuiltInParameter.ROOF_SLOPE)}"
        );

      slope = (headOffset - tailOffset) / lineLength;
    }

    return headOffset;
  }
}
