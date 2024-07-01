using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Circle), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBCircleToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<ADB.Circle, SOG.Circle> _circleConverter;

  public DBCircleToSpeckleConverter(ITypedConverter<ADB.Circle, SOG.Circle> circleConverter)
  {
    _circleConverter = circleConverter;
  }

  public Base Convert(object target) => RawConvert((ADB.Circle)target);

  public SOG.Circle RawConvert(ADB.Circle target) => _circleConverter.Convert(target);
}
