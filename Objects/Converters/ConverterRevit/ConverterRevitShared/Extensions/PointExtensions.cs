using System;
using Objects.Geometry;

namespace ConverterRevitShared.Extensions
{
  internal static class PointExtensions
  {
    public static bool IsOnLineBetweenPoints(this Point middle, Point p0, Point p1, double tolerance = .01)
    {
      // Calculate vectors AB and AP
      double vectorAB_x = p1.x - p0.x;
      double vectorAB_y = p1.y - p0.y;
      double vectorAB_z = p1.z - p0.z;
      double vectorAP_x = middle.x - p0.x;
      double vectorAP_y = middle.y - p0.y;
      double vectorAP_z = middle.z - p0.z;

      // Calculate the cross product
      double crossProductX = vectorAB_y * vectorAP_z - vectorAB_z * vectorAP_y;
      double crossProductY = vectorAB_z * vectorAP_x - vectorAB_x * vectorAP_z;
      double crossProductZ = vectorAB_x * vectorAP_y - vectorAB_y * vectorAP_x;

      // Check if the cross product vector is within tolerance
      return Math.Abs(crossProductX) < tolerance
        && Math.Abs(crossProductY) < tolerance
        && Math.Abs(crossProductZ) < tolerance;
    }
  }
}
