using System.Collections.Generic;

namespace Speckle.Converters.Autocad.Extensions;

public static class ListExtensions
{
  public static SOG.Polyline ConvertToSpecklePolyline(this List<double> pointList, string speckleUnits)
  {
    // throw if list is malformed
    if (pointList.Count % 3 != 0)
    {
      throw new System.ArgumentException("Point list of xyz values is malformed.");
    }

    return new(pointList, speckleUnits);
  }

  public static List<AG.Point2d> ConvertToPoint2d(this List<double> pointList, double conversionFactor = 1)
  {
    // throw if list is malformed
    if (pointList.Count % 2 != 0)
    {
      throw new System.ArgumentException("Point list of xy values is malformed.");
    }

    List<AG.Point2d> points2d = new(pointList.Count / 2);
    for (int i = 1; i < pointList.Count; i += 2)
    {
      points2d.Add(new AG.Point2d(pointList[i - 1] * conversionFactor, pointList[i] * conversionFactor));
    }

    return points2d;
  }

  public static List<AG.Point3d> ConvertToPoint3d(this List<double> pointList, double conversionFactor = 1)
  {
    // throw if list is malformed
    if (pointList.Count % 3 != 0)
    {
      throw new System.ArgumentException("Point list of xyz values is malformed.");
    }

    List<AG.Point3d> points3d = new(pointList.Count / 3);
    for (int i = 2; i < pointList.Count; i += 3)
    {
      points3d.Add(
        new AG.Point3d(
          pointList[i - 2] * conversionFactor,
          pointList[i - 1] * conversionFactor,
          pointList[i] * conversionFactor
        )
      );
    }

    return points3d;
  }
}
