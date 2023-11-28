using System;
using Objects.Geometry;

namespace ConverterRevitShared.Extensions;

internal static class PointExtensions
{
  public static bool IsOnLineBetweenPoints(this Point middle, Point p0, Point p1, double tolerance = .01)
  {
    Vector outerPointsDirection = new(p1 - p0);
    Vector middleToStartPointsDirection = new(middle - p0);

    Vector crossProduct = Vector.CrossProduct(outerPointsDirection, middleToStartPointsDirection);

    // Check if the cross product vector is within tolerance
    return Math.Abs(crossProduct.x) < tolerance
      && Math.Abs(crossProduct.y) < tolerance
      && Math.Abs(crossProduct.z) < tolerance;
  }
}
