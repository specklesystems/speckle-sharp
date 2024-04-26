using System.Collections.Generic;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Autocad2023.ToHost.Raw;

public class AutocadPolycurveToHostPolylineRawConverter : IRawConversion<SOG.Autocad.AutocadPolycurve, ADB.Polyline>
{
  public ADB.Polyline RawConvert(SOG.Autocad.AutocadPolycurve target)
  {
    var points2d = new List<AG.Point2d>();
    var points3d = new List<AG.Point3d>();

    for (int i = 2; i < target.value.Count; i += 3)
    {
      points2d.Add(new AG.Point2d(target.value[i - 2], target.value[i - 1]));
      points3d.Add(new AG.Point3d(target.value[i - 2], target.value[i - 1], target.value[i]));
    }

    ADB.Polyline polyline = new();
    for (int i = 0; i < points2d.Count; i++)
    {
      var bulge = target.bulges is null ? 0 : target.bulges[i];
      polyline.AddVertexAt(i, points2d[i], bulge, 0, 0);
    }

    return polyline;
  }
}
