using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.Autocad.Geometry;

[NameAndRankValue(nameof(ADB.Solid3d), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class Solid3dToSpeckleConverter : IToSpeckleTopLevelConverter
{
  private readonly ITypedConverter<ADB.Solid3d, SOG.Mesh> _solidConverter;

  public Solid3dToSpeckleConverter(ITypedConverter<ADB.Solid3d, SOG.Mesh> solidConverter)
  {
    _solidConverter = solidConverter;
  }

  public Base Convert(object target) => RawConvert((ADB.Solid3d)target);

  public SOG.Mesh RawConvert(ADB.Solid3d target)
  {
    return _solidConverter.Convert(target);
  }
}
