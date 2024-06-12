using Objects;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

// POC: ModelCurve looks a bit bogus and we may wish to revise what that is and how it inherits
// see https://spockle.atlassian.net/browse/CNX-9381
[NameAndRankValue(nameof(IRevitModelCurve), 0)]
public class ModelCurveToSpeckleTopLevelConverter
  : BaseTopLevelConverterToSpeckle<IRevitModelCurve, SOBR.Curve.ModelCurve>
{
  private readonly ITypedConverter<IRevitCurve, ICurve> _curveConverter;
  private readonly IConversionContextStack<IRevitDocument, IRevitForgeTypeId> _conversionContext;

  public ModelCurveToSpeckleTopLevelConverter(
    ITypedConverter<IRevitCurve, ICurve> curveConverter,
    IConversionContextStack<IRevitDocument, IRevitForgeTypeId> conversionContext
  )
  {
    _curveConverter = curveConverter;
    _conversionContext = conversionContext;
  }

  public override SOBR.Curve.ModelCurve Convert(IRevitModelCurve target)
  {
    var modelCurve = new SOBR.Curve.ModelCurve()
    {
      baseCurve = _curveConverter.Convert(target.GeometryCurve),
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
