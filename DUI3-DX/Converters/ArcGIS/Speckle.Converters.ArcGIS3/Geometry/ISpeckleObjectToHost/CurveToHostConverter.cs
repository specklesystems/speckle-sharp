using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3.Geometry.ISpeckleObjectToHost;

[NameAndRankValue(nameof(SOG.Curve), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class CurveToHostConverter : IToHostTopLevelConverter, ITypedConverter<SOG.Curve, ACG.Polyline>
{
  private readonly IRootToHostConverter _converter;

  public CurveToHostConverter(IRootToHostConverter converter)
  {
    _converter = converter;
  }

  public object Convert(Base target) => Convert((SOG.Curve)target);

  public ACG.Polyline Convert(SOG.Curve target)
  {
    // before we have a better idea how to recreate periodic curve
    SOG.Polyline segment = target.displayValue;
    return (ACG.Polyline)_converter.Convert(segment);
  }
}
