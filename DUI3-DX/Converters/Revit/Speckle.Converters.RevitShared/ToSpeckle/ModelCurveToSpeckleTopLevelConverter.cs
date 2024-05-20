using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.Helpers;

namespace Speckle.Converters.RevitShared.ToSpeckle;

// POC: ModelCurve looks a bit bogus and we may wish to revise what that is and how it inherits
// see https://spockle.atlassian.net/browse/CNX-9381
[NameAndRankValue(nameof(DB.ModelCurve), 0)]
public class ModelCurveToSpeckleTopLevelConverter : BaseConversionToSpeckle<DB.ModelCurve, SOBR.Curve.ModelCurve>
{
  private readonly ITypedConverter<DB.Curve, ICurve> _curveConverter;
  private readonly IRevitConversionContextStack _conversionContext;

  public ModelCurveToSpeckleTopLevelConverter(
    ITypedConverter<DB.Curve, ICurve> curveConverter,
    IRevitConversionContextStack conversionContext
  )
  {
    _curveConverter = curveConverter;
    _conversionContext = conversionContext;
  }

  public override SOBR.Curve.ModelCurve RawConvert(DB.ModelCurve target)
  {
    var modelCurve = new SOBR.Curve.ModelCurve()
    {
      baseCurve = _curveConverter.RawConvert(target.GeometryCurve),
      lineStyle = target.LineStyle.Name,
      elementId = target.Id.ToString(),
      units = _conversionContext.Current.SpeckleUnits
    };

    // POC: check this is not going to set the display value to anything we cannot actually display - i.e. polycurve
    // also we have a class for doing this, but probably this is fine for now. see https://spockle.atlassian.net/browse/CNX-9381
    modelCurve["@displayValue"] = modelCurve.baseCurve;

    return modelCurve;
  }
}
