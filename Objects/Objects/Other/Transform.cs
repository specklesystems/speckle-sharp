using System;
using System.Collections.Generic;
using System.Numerics;

using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

using Objects.Geometry;
using Vector = Objects.Geometry.Vector;

namespace Objects.Other
{

  /// <summary>
  /// Generic transform class
  /// </summary>
  public class Transform : Base
  {
    [Obsolete("Use the matrix property")]
    public double[] value { get; set; } = { 1d, 0d, 0d, 0d,
                                             0d, 1d, 0d, 0d,
                                             0d, 0d, 1d, 0d,
                                             0d, 0d, 0d, 1d };

    /// <summary>
    /// The column-based 4x4 transform matrix
    /// </summary>
    /// <remarks>
    /// Graphics based apps typically use column-based matrices, where the last column defines translation. 
    /// Modelling apps may use row-based matrices, where the last row defines translation. Transpose if so.
    /// </remarks>
    public Matrix4x4 matrix { get; set; } = Matrix4x4.Identity;

    /// <summary>
    /// Units for translation
    /// </summary>
    public string units { get; set; }

    [JsonIgnore]
    [Obsolete("Use Decompose method")]
    public double[] translation => value.Subset(3, 7, 11, 15);

    [JsonIgnore]
    [Obsolete("Use Decompose method")]
    public double rotationZ
    {

      get
      {

        var matrix = new Matrix4x4(
      (float)value[0], (float)value[1], (float)value[2], (float)value[3],
      (float)value[4], (float)value[5], (float)value[6], (float)value[7],
      (float)value[8], (float)value[9], (float)value[10], (float)value[11],
      (float)value[12], (float)value[13], (float)value[14], (float)value[15]);

        if (Matrix4x4.Decompose(matrix, out Vector3 _scale, out Quaternion _rotation, out Vector3 _translation))
        {
          return Math.Acos(_rotation.W) * 2;
        }
        else
        {
          return 0;
        }
      }
    }

    [JsonIgnore]
    [Obsolete("Use Decompose method")]
    public double[] scaling => value.Subset(0, 1, 2, 4, 5, 6, 8, 9, 10);

    [JsonIgnore]
    [Obsolete("Evaluate Matrix4x4 matrix directly")]
    public bool isIdentity => value[0] == 1d && value[5] == 1d && value[10] == 1d && value[15] == 1d &&
                              value[1] == 0d && value[2] == 0d && value[3] == 0d &&
                              value[4] == 0d && value[6] == 0d && value[7] == 0d &&
                              value[8] == 0d && value[9] == 0d && value[11] == 0d &&
                              value[12] == 0d && value[13] == 0d && value[14] == 0d;

    [JsonIgnore]
    [Obsolete("Evaluate from Decompose method")]
    public bool isScaled => !(value[0] == 1d && value[5] == 1d && value[10] == 1d);

    public Transform() { }


    public Transform(double[] value, string units = null)
    {
      if (value.Length != 16)
        throw new SpeckleException($"{nameof(Transform)}.{nameof(value)} array is malformed: expected length to be 16");

      this.matrix = new Matrix4x4(
        (float)value[0], (float)value[1], (float)value[2], (float)value[3],
        (float)value[4], (float)value[5], (float)value[6], (float)value[7],
        (float)value[8], (float)value[9], (float)value[10], (float)value[11],
        (float)value[12], (float)value[13], (float)value[14], (float)value[15]
        );
      this.units = units;
    }

    public Transform(Matrix4x4 matrix, string units = null)
    {
      this.matrix = matrix;
      this.units = units;
    }

    /// <summary>
    /// Construct a transform given the x, y, and z bases and the translation vector
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="translation"></param>
    public Transform(Vector x, Vector y, Vector z, Vector translation)
    {
      this.matrix = new Matrix4x4(
       (float)x.x, (float)y.x, (float)z.x, (float)translation.x,
       (float)x.y, (float)y.y, (float)z.y, (float)translation.y,
       (float)x.z, (float)y.z, (float)z.z, (float)translation.z,
       0f, 0f, 0f, 1f
       );
      this.units = translation.units;
    }

    /// <summary>
    /// Decomposes matrix into its scaling, rotation, and translation components
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="rotation"></param>
    /// <param name="translation"></param>
    /// <returns>True if successful, false otherwise</returns>
    public bool Decompose(out Vector scale, out Quaternion rotation, out Vector translation)
    {
      scale = null;
      rotation = Quaternion.Identity;
      translation = null;

      if (Matrix4x4.Decompose(matrix, out Vector3 _scale, out rotation, out Vector3 _translation))
      {
        scale = new Vector(_scale.X, _scale.Y, _scale.Z) { units = Units.None };
        translation = new Vector(_translation.X, _translation.Y, _translation.Z) { units = units };
        return true;
      }
      return false;
    }

    /// <summary>
    /// Converts this transform to the input units
    /// </summary>
    /// <param name="units"></param>
    /// <returns>A matrix with the translation scaled by input units</returns>
    public Transform ConvertTo(string newUnits)
    {
      var newMatrix = matrix;
      var unitFactor = (units != null && newUnits != null) ? (float)Units.GetConversionFactor(units, newUnits) : 1f;
      newMatrix.Translation = new Vector3(matrix.Translation.X * unitFactor, matrix.Translation.Y * unitFactor, matrix.Translation.Z * unitFactor);
      return new Transform(newMatrix, newUnits);
    }

    /// <summary>
    /// Multiplies two transform matrices together
    /// </summary>
    /// <param name="t1">The first transform</param>
    /// <param name="t2">The second transform</param>
    /// <returns>A transform matrix with the units of the first transform</returns>
    public static Transform operator *(Transform t1, Transform t2)
    {
      var convertedTransform = t2.ConvertTo(t1.units);
      var newMatrix = t1.matrix * convertedTransform.matrix;
      return new Transform(newMatrix, t1.units);
    }

    [Obsolete("Use transform method in Point class")]
    /// <summary>
    /// Transform a flat list of doubles representing points
    /// </summary>
    public List<double> ApplyToPoints(List<double> points)
    {
      if (points.Count % 3 != 0)
        throw new SpeckleException(
          $"Cannot apply transform as the points list is malformed: expected length to be multiple of 3");
      var transformed = new List<double>(points.Count);
      for (var i = 0; i < points.Count; i += 3)
        transformed.AddRange(ApplyToPoint(new List<double>(3) { points[i], points[i + 1], points[i + 2] }));

      return transformed;
    }

    [Obsolete("Use transform method in Point class")]
    /// <summary>
    /// Transform a flat list of speckle Points
    /// </summary>
    public List<Point> ApplyToPoints(List<Point> points)
    {
      var transformed = new List<Point>(points.Count);
      for (var i = 0; i < points.Count; i++)
        transformed.Add(ApplyToPoint(points[i]));

      return transformed;
    }

    [Obsolete("Use transform method in Point class")]
    /// <summary>
    /// Transform a single speckle Point
    /// </summary>
    public Point ApplyToPoint(Point point)
    {
      // TODO: move to point class
      if (point == null) return null;

      var unitFactor = units != null ? Units.GetConversionFactor(units, point.units) : 1; // applied to translation vector
      var divisor = matrix.M41 + matrix.M42 + matrix.M43 + unitFactor * matrix.M44;
      var x = (point.x * matrix.M11 + point.y * matrix.M12 + point.z * matrix.M13 + unitFactor * matrix.M14) / divisor;
      var y = (point.x * matrix.M21 + point.y * matrix.M22 + point.z * matrix.M23 + unitFactor * matrix.M24) / divisor;
      var z = (point.x * matrix.M31 + point.y * matrix.M32 + point.z * matrix.M33 + unitFactor * matrix.M34) / divisor;

      var transformed = new Point(x, y, z) { units = point.units, applicationId = point.applicationId };
      return transformed;
    }

    [Obsolete("Use transform method in Point class")]
    /// <summary>
    /// Transform a list of three doubles representing a point
    /// </summary>
    public List<double> ApplyToPoint(List<double> point)
    {
      var transformed = new List<double>();
      for (var i = 0; i < 16; i += 4)
        transformed.Add(point[0] * value[i] + point[1] * value[i + 1] + point[2] * value[i + 2] +
                        value[i + 3]);

      return new List<double>(3)
      {
        transformed[ 0 ] / transformed[ 3 ], transformed[ 1 ] / transformed[ 3 ], transformed[ 2 ] / transformed[ 3 ]
      };
    }

    [Obsolete("Use transform method in Vector class")]
    /// <summary>
    /// Transform a single speckle Vector
    /// </summary>
    public Vector ApplyToVector(Vector vector)
    {
      var newCoords = ApplyToVector(new List<double> { vector.x, vector.y, vector.z });

      return new Geometry.Vector(newCoords[0], newCoords[1], newCoords[2], vector.units, vector.applicationId);
    }

    [Obsolete("Use transform method in Vector class")]
    /// <summary>
    /// Transform a list of three doubles representing a vector
    /// </summary>
    public List<double> ApplyToVector(List<double> vector)
    {
      var newPoint = new List<double>();

      for (var i = 0; i < 12; i += 4)
        newPoint.Add(vector[0] * value[i] + vector[1] * value[i + 1] + vector[2] * value[i + 2]);

      return newPoint;
    }

    [Obsolete("Use transform method in Curve class")]
    /// <summary>
    /// Transform a flat list of ICurves. Note that if any of the ICurves does not implement `ITransformable`,
    /// it will not be returned.
    /// </summary>
    public List<ICurve> ApplyToCurves(List<ICurve> curves, out bool success)
    {
      success = true;
      var transformed = new List<ICurve>();
      foreach (var curve in curves)
      {
        if (curve is ITransformable c)
        {
          c.TransformTo(this, out ITransformable tc);
          transformed.Add((ICurve)tc);
        }
        else
          success = false;
      }

      return transformed;
    }
    
  }

  [Obsolete]
  static class ArrayUtils
  {
    // create a subset from a specific list of indices
    public static T[] Subset<T>(this T[] array, params int[] indices)
    {
      var subset = new T[indices.Length];
      for (var i = 0; i < indices.Length; i++)
      {
        subset[i] = array[indices[i]];
      }

      return subset;
    }
  }
}
