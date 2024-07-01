using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.ToSpeckle.Geometry;

[NameAndRankValue(nameof(ADB.Arc), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class DBArcToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<ADB.Arc, SOG.Arc> _arcConverter;

  public DBArcToSpeckleConverter(ITypedConverter<ADB.Arc, SOG.Arc> arcConverter)
  {
    _arcConverter = arcConverter;
  }

  public Base Convert(object target) => Convert((ADB.Arc)target);

  public SOG.Arc Convert(ADB.Arc target) => _arcConverter.Convert(target);
}
