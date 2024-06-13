using Objects.BuiltElements.Revit.RevitRoof;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Extensions;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

[NameAndRankValue(nameof(IRevitExtrusionRoof), 0)]
public class ExtrusionRoofToSpeckleTopLevelConverter
  : BaseTopLevelConverterToSpeckle<IRevitExtrusionRoof, RevitExtrusionRoof>
{
  private readonly ITypedConverter<IRevitLevel, SOBR.RevitLevel> _levelConverter;
  private readonly ITypedConverter<IRevitModelCurveArray, SOG.Polycurve> _modelCurveArrayConverter;
  private readonly ITypedConverter<IRevitXYZ, SOG.Point> _pointConverter;
  private readonly IParameterValueExtractor _parameterValueExtractor;
  private readonly IDisplayValueExtractor _displayValueExtractor;
  private readonly IHostedElementConversionToSpeckle _hostedElementConverter;
  private readonly IParameterObjectAssigner _parameterObjectAssigner;
  private readonly IRevitFilterFactory _revitFilterFactory;

  public ExtrusionRoofToSpeckleTopLevelConverter(
    ITypedConverter<IRevitLevel, SOBR.RevitLevel> levelConverter,
    ITypedConverter<IRevitModelCurveArray, SOG.Polycurve> modelCurveArrayConverter,
    ITypedConverter<IRevitXYZ, SOG.Point> pointConverter,
    IParameterValueExtractor parameterValueExtractor,
    IDisplayValueExtractor displayValueExtractor,
    IHostedElementConversionToSpeckle hostedElementConverter,
    IParameterObjectAssigner parameterObjectAssigner,
    IRevitFilterFactory revitFilterFactory
  )
  {
    _levelConverter = levelConverter;
    _modelCurveArrayConverter = modelCurveArrayConverter;
    _pointConverter = pointConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
    _revitFilterFactory = revitFilterFactory;
  }

  public override RevitExtrusionRoof Convert(IRevitExtrusionRoof target)
  {
    var speckleExtrusionRoof = new RevitExtrusionRoof
    {
      start = _parameterValueExtractor.GetValueAsDouble(target, RevitBuiltInParameter.EXTRUSION_START_PARAM),
      end = _parameterValueExtractor.GetValueAsDouble(target, RevitBuiltInParameter.EXTRUSION_END_PARAM)
    };
    var plane = target.GetProfile()[0].SketchPlane.GetPlane();
    speckleExtrusionRoof.referenceLine = new SOG.Line(
      _pointConverter.Convert(plane.Origin.Add(plane.XVec.Normalize().Negate())),
      _pointConverter.Convert(plane.Origin)
    );
    var level = _parameterValueExtractor.GetValueAsRevitLevel(
      target,
      RevitBuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM
    );
    speckleExtrusionRoof.level = _levelConverter.Convert(level.NotNull());
    speckleExtrusionRoof.outline = _modelCurveArrayConverter.Convert(target.GetProfile());

    var elementType = target.Document.GetElement(target.GetTypeId()).NotNull().ToType().NotNull();
    speckleExtrusionRoof.type = elementType.Name;
    speckleExtrusionRoof.family = elementType.FamilyName;

    _parameterObjectAssigner.AssignParametersToBase(target, speckleExtrusionRoof);
    speckleExtrusionRoof.displayValue = _displayValueExtractor.GetDisplayValue(target);
    speckleExtrusionRoof.elements = _hostedElementConverter
      .ConvertHostedElements(target.GetHostedElementIds(_revitFilterFactory))
      .ToList();

    return speckleExtrusionRoof;
  }
}
