using Objects.Geometry;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;

namespace Speckle.Converters.ArcGIS3;

[NameAndRankValue(nameof(String), 0)]
public class FakeConversion : IHostObjectToSpeckleConversion, IRawConversion<String, Point>
{
  public Base Convert(object target) => RawConvert((String)target);

  public Point RawConvert(String target)
  {
    return new Point(0, 0, 100) { ["customText"] = target };
  }
}
