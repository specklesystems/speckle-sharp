using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Helpers;

[GenerateAutoInterface]
public class SlopeArrowExtractor : ISlopeArrowExtractor
{
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _pointConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly IRevitFilterFactory _revitFilterFactory;

  public SlopeArrowExtractor(
    ITypedConverter<IRevitXYZ, SOG.Point> pointConverter,
    ParameterValueExtractor parameterValueExtractor,
    IRevitFilterFactory revitFilterFactory
  )
  {
    _pointConverter = pointConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _revitFilterFactory = revitFilterFactory;
  }

  public IRevitModelLine? GetSlopeArrow(IRevitElement element)
  {
    IList<IRevitElementId>? elementIds = null;
    if (element is IRevitFloor floor)
    {
      elementIds = (floor.Document.GetElement(floor.SketchId).ToSketch().NotNull()).GetAllElements();
    }

    if (elementIds == null)
    {
      using var modelLineFilter = _revitFilterFactory.CreateElementCategoryFilter(RevitBuiltInCategory.OST_SketchLines);
      elementIds = element.GetDependentElements(modelLineFilter);
    }

    foreach (var elementId in elementIds)
    {
      var line = element.Document.GetElement(elementId).ToModelLine();
      if (line is null)
      {
        continue;
      }

      var offsetAtTailParameter = line.GetParameter(RevitBuiltInParameter.SLOPE_START_HEIGHT);
      if (offsetAtTailParameter != null)
      {
        return line;
      }
    }
    return null;
  }

  public SOG.Point GetSlopeArrowHead(IRevitModelLine slopeArrow)
  {
    return _pointConverter.Convert((slopeArrow.GetLocationAsLocationCurve().NotNull()).Curve.GetEndPoint(1));
  }

  public SOG.Point GetSlopeArrowTail(IRevitModelLine slopeArrow)
  {
    return _pointConverter.Convert((slopeArrow.GetLocationAsLocationCurve().NotNull()).Curve.GetEndPoint(0));
  }

  public double GetSlopeArrowTailOffset(IRevitModelLine slopeArrow)
  {
    return _parameterValueExtractor.GetValueAsDouble(slopeArrow, RevitBuiltInParameter.SLOPE_START_HEIGHT);
  }

  public double GetSlopeArrowHeadOffset(IRevitModelLine slopeArrow, double tailOffset, out double slope)
  {
    var specifyOffset = _parameterValueExtractor.GetValueAsInt(
      slopeArrow,
      RevitBuiltInParameter.SPECIFY_SLOPE_OR_OFFSET
    );

    var lineLength = _parameterValueExtractor.GetValueAsDouble(slopeArrow, RevitBuiltInParameter.CURVE_ELEM_LENGTH);

    slope = 0;
    double headOffset = 0;
    // 1 corrosponds to the "slope" option
    if (specifyOffset == 1)
    {
      // in this scenario, slope is returned as a percentage. Divide by 100 to get the unitless form
      slope = _parameterValueExtractor.GetValueAsDouble(slopeArrow, RevitBuiltInParameter.ROOF_SLOPE) / 100d;
      headOffset = tailOffset + lineLength * Math.Sin(Math.Atan(slope));
    }
    else if (specifyOffset == 0) // 0 corrospondes to the "height at tail" option
    {
      headOffset = _parameterValueExtractor.GetValueAsDouble(slopeArrow, RevitBuiltInParameter.SLOPE_END_HEIGHT);

      slope = (headOffset - tailOffset) / lineLength;
    }

    return headOffset;
  }
}

// POC: why do we need this send selection?
// why does conversion need to know about selection in this way?
public class SendSelection
{
  private readonly HashSet<string> _selectedItemIds;

  public SendSelection(IEnumerable<string> selectedItemIds)
  {
    _selectedItemIds = new HashSet<string>(selectedItemIds);
  }

  public bool Contains(string elementId) => _selectedItemIds.Contains(elementId);

  public IReadOnlyCollection<string> SelectedItems => _selectedItemIds.ToList().AsReadOnly();
}
