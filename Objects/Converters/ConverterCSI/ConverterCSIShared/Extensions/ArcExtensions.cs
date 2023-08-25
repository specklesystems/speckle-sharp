using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;
using Objects.Geometry;

namespace ConverterCSIShared.Extensions
{
  internal static class ArcExtensions
  {
    public static IEnumerable<Point> ToPoints(this Arc arc)
    {
      var startPoint = new Vector3((float)arc.startPoint.x, (float)arc.startPoint.y, (float)arc.startPoint.z);
      var endPoint = new Vector3((float)arc.endPoint.x, (float)arc.endPoint.y, (float)arc.endPoint.z);
      var midPoint = new Vector3((float)arc.midPoint.x, (float)arc.midPoint.y, (float)arc.midPoint.z);

      // Calculate the radius and center of the arc
      Vector3 center = CalculateArcCenter(startPoint, midPoint, endPoint);
      float radius = Vector3.Distance(startPoint, center);

      // Calculate the plane defined by the start, center, and end points
      Vector3 normal = Vector3.Normalize(Vector3.Cross(startPoint - center, endPoint - center));

      // Calculate the angles between the start, center, and end points
      float startAngle = CalculateAngle(center, startPoint, normal);
      float endAngle = CalculateAngle(center, endPoint, normal);
      float angleRange = endAngle - startAngle;

      // Calculate the angular increment for each point
      float angularIncrement = angleRange / 9; // 10 points including start and end points

      // Generate the arc points
      for (int i = 0; i <= 10; i++)
      {
        float currentAngle = startAngle + (i * angularIncrement);
        Vector3 point = RotatePoint(startPoint, center, normal, currentAngle);
        yield return new Point(point.X, point.Y, point.Z);
      }
    }

    private static Vector3 CalculateArcCenter(Vector3 startPoint, Vector3 midPoint, Vector3 endPoint)
    {
      // Calculate the perpendicular bisectors of the line segments between start-mid and mid-end
      Vector3 startMidMidpoint = (startPoint + midPoint) / 2;
      Vector3 midEndMidpoint = (midPoint + endPoint) / 2;
      Vector3 startMidDirection = Vector3.Normalize(midPoint - startPoint);
      Vector3 midEndDirection = Vector3.Normalize(endPoint - midPoint);

      // Calculate the normal to the plane containing the bisectors
      Vector3 normal = Vector3.Normalize(Vector3.Cross(startMidDirection, midEndDirection));

      // Calculate the intersection point of the plane and the perpendicular bisectors (arc center)
      float startMidDot = Vector3.Dot(startMidMidpoint, normal);
      float midEndDot = Vector3.Dot(midEndMidpoint, normal);
      float normalDot = Vector3.Dot(normal, normal);
      float t = (startMidDot - midEndDot) / normalDot;
      Vector3 center = startMidMidpoint - t * normal;

      return center;
    }

    private static float CalculateAngle(Vector3 center, Vector3 point, Vector3 normal)
    {
      Vector3 direction = point - center;
      float angle = (float)Math.Atan2(Vector3.Dot(normal, Vector3.Cross(center, point)), Vector3.Dot(center, point));
      return angle;
    }

    private static Vector3 RotatePoint(Vector3 point, Vector3 center, Vector3 axis, float angle)
    {
      Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angle);
      Vector3 rotatedPoint = Vector3.Transform(point - center, rotation);
      return rotatedPoint + center;
    }
  }
}
