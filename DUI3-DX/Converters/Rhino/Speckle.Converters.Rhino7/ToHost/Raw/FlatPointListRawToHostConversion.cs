using Rhino.Collections;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Logging;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class FlatPointListRawToHostConversion : IRawConversion<IList<double>, Point3dList>
{
  public Point3dList RawConvert(IList<double> target)
  {
    if (target.Count % 3 != 0)
    {
      throw new SpeckleException("Array malformed: length%3 != 0.");
    }

    var points = new List<RG.Point3d>(target.Count / 3);
    var scaleFactor = 1; //POC: Missing unit conversion, previously -> Units.GetConversionFactor(units, ModelUnits);

    for (int i = 2; i < target.Count; i += 3)
    {
      points.Add(new RG.Point3d(target[i - 2] * scaleFactor, target[i - 1] * scaleFactor, target[i] * scaleFactor));
    }

    return new Point3dList(points);
  }
}
