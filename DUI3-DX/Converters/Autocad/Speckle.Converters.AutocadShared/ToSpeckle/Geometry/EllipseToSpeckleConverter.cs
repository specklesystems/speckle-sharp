using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Ellipse), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBEllipseToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<ADB.Ellipse, SOG.Ellipse> _ellipseConverter;

  public DBEllipseToSpeckleConverter(ITypedConverter<ADB.Ellipse, SOG.Ellipse> ellipseConverter)
  {
    _ellipseConverter = ellipseConverter;
  }

  public Base Convert(object target) => RawConvert((ADB.Ellipse)target);

  public SOG.Ellipse RawConvert(ADB.Ellipse target) => _ellipseConverter.Convert(target);
}
