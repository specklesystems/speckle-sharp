using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3;

[NameAndRankValue(nameof(String), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class FakeConversion : IHostObjectToSpeckleConversion, ITypedConverter<String, Point>
{
  public Base Convert(object target) => Convert((String)target);

  public Point Convert(String target)
  {
    return new Point(0, 0, 100) { ["customText"] = target };
  }
}
