using Speckle.Converters.Common.Objects;
using Speckle.Converters.Common;
using Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: needs review feels, BIG, feels like it could be broken down..
// i.e. GetParams(), GetGeom()? feels like it's doing too much
[NameAndRankValue(nameof(DBA.TopographySurface), 0)]
public class TopographyConversionToSpeckle : BaseConversionToSpeckle<DBA.TopographySurface, SOBR.RevitTopography>
{
  private readonly DisplayValueExtractor _displayValueExtractor;
  private readonly IRawConversion<DB.Curve, ICurve> _curveConverter;
  private readonly IRawConversion<DB.Level, SOBR.RevitLevel> _levelConverter;
  private readonly IRawConversion<DB.CurveArray, SOG.Polycurve> _curveArrayConverter;
  private readonly ParameterValueExtractor _parameterValueExtractor;
  private readonly RevitConversionContextStack _contextStack;

  private readonly HostedElementConversionToSpeckle _hostedElementConverter;
  private readonly ParameterObjectAssigner _parameterObjectAssigner;

  public TopographyConversionToSpeckle(
    IRawConversion<DB.Curve, ICurve> curveConverter,
    IRawConversion<DB.Level, SOBR.RevitLevel> levelConverter,
    RevitConversionContextStack contextStack,
    ParameterValueExtractor parameterValueExtractor,
    DisplayValueExtractor displayValueExtractor,
    IRawConversion<DB.CurveArray, SOG.Polycurve> curveArrayConverter,
    HostedElementConversionToSpeckle hostedElementConverter,
    ParameterObjectAssigner parameterObjectAssigner
  )
  {
    _curveConverter = curveConverter;
    _levelConverter = levelConverter;
    _contextStack = contextStack;
    _parameterValueExtractor = parameterValueExtractor;
    _displayValueExtractor = displayValueExtractor;
    _curveArrayConverter = curveArrayConverter;
    _hostedElementConverter = hostedElementConverter;
    _parameterObjectAssigner = parameterObjectAssigner;
  }

  public override SOBR.RevitTopography RawConvert(DBA.TopographySurface target)
  {
    var speckleTopo = new SOBR.RevitTopography();
    speckleTopo.displayValue = _displayValueExtractor.GetDisplayValue(target);

    // POC: shouldn't we just do this in the RevitConverterToSpeckle ?
    _parameterObjectAssigner.AssignParametersToBase(target, speckleTopo);

    return speckleTopo;
  }
}
