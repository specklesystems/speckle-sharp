using Objects.Geometry;
using Speckle.Converters.Common.Objects;
using Speckle.Revit.Interfaces;
#pragma warning disable IDE0130
namespace Speckle.Converters.Revit2023;

internal sealed class ModelCurveArrArrayConverterToSpeckle : ITypedConverter<IRevitModelCurveArrArray, Polycurve[]>
{
  private readonly ITypedConverter<IRevitModelCurveArray, Objects.Geometry.Polycurve> _modelCurveArrayConverter;

  public ModelCurveArrArrayConverterToSpeckle(
    ITypedConverter<IRevitModelCurveArray, Objects.Geometry.Polycurve> modelCurveArrayConverter
  )
  {
    _modelCurveArrayConverter = modelCurveArrayConverter;
  }

  public Objects.Geometry.Polycurve[] Convert(IRevitModelCurveArrArray target)
  {
    var polycurves = new Objects.Geometry.Polycurve[target.Count];
    var revitArrays = target.ToArray();

    for (int i = 0; i < polycurves.Length; i++)
    {
      polycurves[i] = _modelCurveArrayConverter.Convert(revitArrays[i]);
    }

    return polycurves;
  }
}
