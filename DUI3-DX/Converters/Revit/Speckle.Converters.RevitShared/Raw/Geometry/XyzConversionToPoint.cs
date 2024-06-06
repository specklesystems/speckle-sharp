using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class XyzConversionToPoint  : ConverterAdapter<DB.XYZ, IRevitXYZ, XYZProxy>
{
  public XyzConversionToPoint(ITypedConverter<IRevitXYZ, Base> converter) : base(converter)
  {
  }

  protected override XYZProxy Create(DB.XYZ target) => new (target);
}
