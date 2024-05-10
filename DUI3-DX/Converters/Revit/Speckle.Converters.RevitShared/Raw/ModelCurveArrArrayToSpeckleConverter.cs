using Autodesk.Revit.DB;
using Objects.Geometry;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.RevitShared.Raw;

internal sealed class ModelCurveArrArrayConverterToSpeckle : IRawConversion<DB.ModelCurveArrArray, SOG.Polycurve[]>
{
  private readonly IRawConversion<DB.ModelCurveArray, SOG.Polycurve> _modelCurveArrayConverter;

  public ModelCurveArrArrayConverterToSpeckle(IRawConversion<ModelCurveArray, Polycurve> modelCurveArrayConverter)
  {
    _modelCurveArrayConverter = modelCurveArrayConverter;
  }

  public SOG.Polycurve[] RawConvert(ModelCurveArrArray target)
  {
    var polycurves = new Polycurve[target.Size];
    var revitArrays = target.Cast<ModelCurveArray>().ToArray();

    for (int i = 0; i < polycurves.Length; i++)
    {
      polycurves[i] = _modelCurveArrayConverter.RawConvert(revitArrays[i]);
    }

    return polycurves;
  }
}
