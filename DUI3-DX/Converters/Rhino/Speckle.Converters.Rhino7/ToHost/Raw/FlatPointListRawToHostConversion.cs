using Rhino.Collections;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class FlatPointListRawToHostConversion : IRawConversion<IReadOnlyList<double>, Point3dList>
{
  public Point3dList RawConvert(IReadOnlyList<double> target)
  {
    if (target.Count % 3 != 0)
    {
      throw new SpeckleException("Array malformed: length%3 != 0.");
    }

    var points = new List<RG.Point3d>(target.Count / 3);

    for (int i = 2; i < target.Count; i += 3)
    {
      points.Add(new RG.Point3d(target[i - 2], target[i - 1], target[i]));
    }

    return new Point3dList(points);
  }
}
