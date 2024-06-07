using Speckle.Converters.Common.Objects;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.RevitShared.ToSpeckle;

public class XyzConversionToPoint  : ConverterAdapter<DB.XYZ, IRevitXYZ, XYZProxy, SOG.Point>
{
  public XyzConversionToPoint(ITypedConverter<IRevitXYZ, SOG.Point> converter) : base(converter)
  {
  }

  protected override XYZProxy Create(DB.XYZ target) => new (target);
}
