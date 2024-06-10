using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.ToSpeckle;

public sealed class CurveArrArrayToSpecklePolycurveConverter : ITypedConverter<IRevitCurveArrArray, List<SOG.Polycurve>>
{
  private readonly ITypedConverter<IRevitCurveArray, SOG.Polycurve> _curveArrayConverter;

  public CurveArrArrayToSpecklePolycurveConverter(ITypedConverter<IRevitCurveArray, SOG.Polycurve> curveArrayConverter)
  {
    _curveArrayConverter = curveArrayConverter;
  }

  public List<SOG.Polycurve> Convert(IRevitCurveArrArray target)
  {
    List<SOG.Polycurve> polycurves = new(target.Count);
    foreach (var curveArray in target)
    {
      polycurves.Add(_curveArrayConverter.Convert(curveArray));
    }

    return polycurves;
  }
}
