using Autodesk.Revit.DB;
using Objects;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

[NameAndRankValue(nameof(DB.Ceiling), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
internal sealed class CeilingTopLevelConverterToSpeckle : BaseTopLevelConverterToSpeckle<DB.Ceiling, SOBR.RevitCeiling>
{
  private readonly ITypedConverter<DB.CurveArrArray, List<SOG.Polycurve>> _curveArrArrayConverter;
  private readonly ITypedConverter<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;
  private readonly DisplayValueExtractor _displayValueExtractor;

  //private readonly HostedElementConversionToSpeckle _hostedElementConverter;

  public CeilingTopLevelConverterToSpeckle(
    ITypedConverter<CurveArrArray, List<Polycurve>> curveArrArrayConverter,
    ITypedConverter<DB.Level, RevitLevel> levelConverter,
    ParameterValueExtractor parameterValueExtractor,
    ParameterObjectAssigner parameterObjectAssigner,
    DisplayValueExtractor displayValueExtractor
  )
  {
    _curveArrArrayConverter = curveArrArrayConverter;
    _levelConverter = levelConverter;
    _parameterValueExtractor = parameterValueExtractor;
    _parameterObjectAssigner = parameterObjectAssigner;
    _displayValueExtractor = displayValueExtractor;
  }

  public override RevitCeiling Convert(DB.Ceiling target)
  {
    var sketch = (Sketch)target.Document.GetElement(target.SketchId);
    List<SOG.Polycurve> profiles = _curveArrArrayConverter.Convert(sketch.Profile);

    var speckleCeiling = new RevitCeiling();

    var elementType = (ElementType)target.Document.GetElement(target.GetTypeId());
    speckleCeiling.type = elementType.Name;
    speckleCeiling.family = elementType.FamilyName;

    // POC: https://spockle.atlassian.net/browse/CNX-9396
    if (profiles.Count > 0)
    {
      speckleCeiling.outline = profiles[0];
    }
    if (profiles.Count > 1)
    {
      speckleCeiling.voids = profiles.Skip(1).ToList<ICurve>();
    }

    // POC: our existing receive operation is checking the "slopeDirection" prop,
    // but it is never being set. We should be setting it

    var level = _parameterValueExtractor.GetValueAsDocumentObject<DB.Level>(target, DB.BuiltInParameter.LEVEL_PARAM);
    speckleCeiling.level = _levelConverter.Convert(level);

    _parameterObjectAssigner.AssignParametersToBase(target, speckleCeiling);
    speckleCeiling.displayValue = _displayValueExtractor.GetDisplayValue(target);

    // POC: hosted elements OOS for alpha, but this exists in existing connector
    //_hostedElementConverter.AssignHostedElements(target, speckleCeiling);

    return speckleCeiling;
  }
}
