using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Raw;

internal sealed class ModelCurveArrArrayConverterToSpeckle : ConverterAdapter<DB.ModelCurveArrArray, IRevitModelCurveArrArray, ModelCurveArrArrayProxy, SOG.Polycurve[]>
{
  public ModelCurveArrArrayConverterToSpeckle(ITypedConverter<IRevitModelCurveArrArray, SOG.Polycurve[]> converter) : base(converter)
  {
  }

  protected override ModelCurveArrArrayProxy Create(ModelCurveArrArray target) => new (target);
}
