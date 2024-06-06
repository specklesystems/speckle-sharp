using Autodesk.Revit.DB;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Core.Models;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Raw;

internal sealed class ModelCurveArrArrayConverterToSpeckle : ConverterAdapter<DB.ModelCurveArrArray, IRevitModelCurveArrArray, ModelCurveArrArrayProxy>
{
  public ModelCurveArrArrayConverterToSpeckle(ITypedConverter<IRevitModelCurveArrArray, Base> converter) : base(converter)
  {
  }

  protected override ModelCurveArrArrayProxy Create(ModelCurveArrArray target) => new (target);
}
