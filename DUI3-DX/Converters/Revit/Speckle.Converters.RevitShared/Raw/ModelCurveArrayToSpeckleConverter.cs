using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Raw;

internal sealed class ModelCurveArrayToSpeckleConverter : ConverterAdapter<DB.ModelCurveArray, IRevitModelCurveArray, ModelCurveArrayProxy, SOG.Polycurve>
{
  public ModelCurveArrayToSpeckleConverter(ITypedConverter<IRevitModelCurveArray, SOG.Polycurve> converter) : base(converter)
  {
  }

  protected override ModelCurveArrayProxy Create(DB.ModelCurveArray target) =>  new (target);
}
