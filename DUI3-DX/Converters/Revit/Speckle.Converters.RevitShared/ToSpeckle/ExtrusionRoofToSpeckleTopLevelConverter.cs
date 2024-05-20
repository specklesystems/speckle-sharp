using Objects.BuiltElements.Revit.RevitRoof;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared.Helpers;

using Speckle.Converters.RevitShared.Extensions;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.ExtrusionRoof), 0)]
public class ExtrusionRoofToSpeckleTopLevelConverter : BaseConversionToSpeckle<DB.ExtrusionRoof, RevitExtrusionRoof>
{
  private readonly ITypedConverter<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly ITypedConverter<DB.ModelCurveArray, SOG.Polycurve> _modelCurveArrayConverter;
  private readonly ITypedConverter<DB.XYZ, SOG.Point> _pointConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public ExtrusionRoofToSpeckleTopLevelConverter(
    ITypedConverter<DB.Level, SOBR.RevitLevel> levelConverter,
    ITypedConverter<DB.ModelCurveArray, SOG.Polycurve> modelCurveArrayConverter,
    ITypedConverter<DB.XYZ, SOG.Point> pointConverter,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    HostedElementConversionToSpeckle hostedElementConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _levelConverter = levelConverter;
    _modelCurveArrayConverter = modelCurveArrayConverter;
    _pointConverter = pointConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public override RevitExtrusionRoof RawConvert(DB.ExtrusionRoof target)
  {
    var speckleExtrusionRoof = new RevitExtrusionRoof
    {
      start = _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.EXTRUSION_START_PARAM),
      end = _parameterValueExtractor.GetValueAsDouble(target, DB.BuiltInParameter.EXTRUSION_END_PARAM)
    };
    var plane = target.GetProfile().get_Item(0).SketchPlane.GetPlane();
    speckleExtrusionRoof.referenceLine = new SOG.Line(
      _pointConverter.RawConvert(plane.Origin.Add(plane.XVec.Normalize().Negate())),
      _pointConverter.RawConvert(plane.Origin)
    );
    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(
      target,
      DB.BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM
    );
    speckleExtrusionRoof.level = _levelConverter.RawConvert(level);
    speckleExtrusionRoof.outline = _modelCurveArrayConverter.RawConvert(target.GetProfile());

    var elementType = (DB.ElementType)target.Document.GetElement(target.GetTypeId());
    speckleExtrusionRoof.type = elementType.Name;
    speckleExtrusionRoof.family = elementType.FamilyName;

    _parameterObjectAssigner.AssignParametersToBase(target, speckleExtrusionRoof);
    speckleExtrusionRoof.displayValue = _displayValueExtractor.GetDisplayValue(target);
    speckleExtrusionRoof.elements = _hostedElementConverter
      .ConvertHostedElements(target.GetHostedElementIds())
      .ToList();

    return speckleExtrusionRoof;
  }
}
