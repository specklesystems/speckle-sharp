using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Core.Models;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.Raw;

internal sealed class ModelCurveArrayToSpeckleConverter : ConverterAdapter<DB.ModelCurveArray, IRevitModelCurveArray, ModelCurveArrayProxy>
{
  public ModelCurveArrayToSpeckleConverter(ITypedConverter<IRevitModelCurveArray, Base> converter) : base(converter)
  {
  }

  protected override ModelCurveArrayProxy Create(DB.ModelCurveArray target) =>  new (target);
}
