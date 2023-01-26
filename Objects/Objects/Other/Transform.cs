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

    public Transform() { }

    /// <summary>
    /// Construct a transform from a row-based double array of size 16
    /// </summary>
    /// <param name="value"></param>
    /// <param name="units"></param>
    /// <exception cref="SpeckleException"></exception>
    public Transform(double[] value, string units = null)
    {
      if (value.Length != 16)
        throw new SpeckleException($"{nameof(Transform)}.{nameof(value)} array is malformed: expected length to be 16");

      this.matrix = GetArrayMatrix(value);
      this.units = units;
    }

    /// <summary>
    /// Construct a transform from a 4x4 matrix and translation units
    /// </summary>
    /// <param name="matrix"></param>
    /// <param name="units"></param>
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
      var unitFactor = (units != null && newUnits != null) ? Units.GetConversionFactor(units, newUnits) : 1d;
      newMatrix.M14 = (float)(matrix.Translation.X * unitFactor);
      newMatrix.M24 = (float)(matrix.Translation.Y * unitFactor);
      newMatrix.M34 = (float)(matrix.Translation.Z * unitFactor);
      return new Transform(newMatrix, newUnits);
    }

    /// <summary>
    /// Returns the dot product of two matrices
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

    // Retrieves a double array from the matrix
    public double[] GetMatrixArray()
    {
      return new double[] {
        matrix.M11, matrix.M12, matrix.M13, matrix.M14,
        matrix.M21, matrix.M22, matrix.M23, matrix.M24,
        matrix.M31, matrix.M32, matrix.M33, matrix.M34,
        matrix.M41, matrix.M42, matrix.M43, matrix.M44
      };
    }

    // Retrieves a matrix from a double array
    public Matrix4x4 GetArrayMatrix(double[] value)
    {
      return new Matrix4x4
      (
        (float)value[0], (float)value[1], (float)value[2], (float)value[3],
        (float)value[4], (float)value[5], (float)value[6], (float)value[7],
        (float)value[8], (float)value[9], (float)value[10], (float)value[11],
        (float)value[12], (float)value[13], (float)value[14], (float)value[15]
      );
    }

    #region obsolete

    [JsonIgnore, Obsolete("Use the matrix property")]
    public double[] value
    {
      get
      {
        return GetMatrixArray();
      }
      set
      {
        matrix = GetArrayMatrix(value);
      }
    }

    [JsonIgnore, Obsolete("Use Decompose method", true)]
    public double rotationZ
    {
      get
      {
        if (Decompose(out Vector scale, out Quaternion rotation, out Vector translation))
        {
          return Math.Acos(rotation.W) * 2;
        }
        else
        {
          return 0;
        }
      }
    }

    [Obsolete("Use transform method in Point class", true)]
    /// <summary>
    /// Transform a flat list of doubles representing points
    /// </summary>
    public List<double> ApplyToPoints(List<double> points)
    {
      if (points.Count % 3 != 0)
        throw new SpeckleException($"Cannot apply transform as the points list is malformed: expected length to be multiple of 3");

      var transformed = new List<double>(points.Count);
      for (var i = 0; i < points.Count; i += 3)
      {
        var point = new Point(points[i], points[i + 1], points[i + 2]);
        point.TransformTo(this, out Point transformedPoint);
        transformed.AddRange(transformedPoint.ToList());
      }
      return transformed;
    }

    [Obsolete("Use transform method in Point class", true)]
    /// <summary>
    /// Transform a flat list of speckle Points
    /// </summary>
    public List<Point> ApplyToPoints(List<Point> points)
    {
      var transformedPoints = new List<Point>();
      foreach (var point in points)
      {
        point.TransformTo(this, out Point transformedPoint);
        transformedPoints.Add(transformedPoint);
      }
      return transformedPoints;
    }

    [Obsolete("Use transform method in Point class", true)]
    /// <summary>
    /// Transform a single speckle Point
    /// </summary>
    public Point ApplyToPoint(Point point)
    {
      if (point == null) return null;

      point.TransformTo(this, out Point transformedPoint);
      return transformedPoint;
    }

    [Obsolete("Use transform method in Point class")]
    /// <summary>
    /// Transform a list of three doubles representing a point
    /// </summary>
    public List<double> ApplyToPoint(List<double> point)
    {
      var _point = new Point(point[0], point[1], point[2]);
      _point.TransformTo(this, out Point transformedPoint);
      return transformedPoint.ToList();
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
      // TODO: move to curve class
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

    #endregion

  }
}
