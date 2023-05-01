using System;
using System.Collections.Generic;
using System.Numerics;
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Vector = Objects.Geometry.Vector;

namespace Objects.Other;

/// <summary>
/// Generic transform class
/// </summary>
public class Transform : Base
{
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
      throw new ArgumentException(
        $"{nameof(Transform)}.{nameof(value)} array is malformed: expected length to be 16",
        nameof(value)
      );

    matrix = CreateMatrix(value);
    this.units = units;
  }

  /// <summary>
  /// Construct a transform from a row-based float array of size 16
  /// </summary>
  /// <param name="value"></param>
  /// <param name="units"></param>
  /// <exception cref="SpeckleException"></exception>
  public Transform(float[] value, string units = null)
  {
    if (value.Length != 16)
      throw new SpeckleException($"{nameof(Transform)}.{nameof(value)} array is malformed: expected length to be 16");

    matrix = CreateMatrix(value);
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
    matrix = new Matrix4x4(
      Convert.ToSingle(x.x),
      Convert.ToSingle(y.x),
      Convert.ToSingle(z.x),
      Convert.ToSingle(translation.x),
      Convert.ToSingle(x.y),
      Convert.ToSingle(y.y),
      Convert.ToSingle(z.y),
      Convert.ToSingle(translation.y),
      Convert.ToSingle(x.z),
      Convert.ToSingle(y.z),
      Convert.ToSingle(z.z),
      Convert.ToSingle(translation.z),
      0f,
      0f,
      0f,
      1f
    );
    units = translation.units;
  }

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

  /// <summary>
  /// Decomposes matrix into its scaling, rotation, and translation components
  /// </summary>
  /// <param name="scale"></param>
  /// <param name="rotation"></param>
  /// <param name="translation"></param>
  /// <returns>True if successful, false otherwise</returns>
  public void Decompose(out Vector3 scale, out Quaternion rotation, out Vector4 translation)
  {
    // translation
    translation = new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44);

    // scale
    // this should account for non-uniform scaling
    var scaleX = new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41).Length();
    var scaleY = new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42).Length();
    var scaleZ = new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43).Length();
    scale = new Vector3(scaleX, scaleY, scaleZ);

    // rotation
    // this is using a z-up convention for basis vectors
    var up = new Vector3(matrix.M13, matrix.M23, matrix.M33);
    var forward = new Vector3(matrix.M12, matrix.M22, matrix.M32);
    rotation = LookRotation(forward, up);
  }

  private static Quaternion LookRotation(Vector3 forward, Vector3 up)
  {
    Vector3 vector = new(forward.X / forward.Length(), forward.Y / forward.Length(), forward.Z / forward.Length());
    Vector3 vector2 = Vector3.Cross(up, forward);
    Vector3 vector3 = Vector3.Cross(vector, vector2);
    var m00 = vector2.X;
    var m01 = vector2.Y;
    var m02 = vector2.Z;
    var m10 = vector3.X;
    var m11 = vector3.Y;
    var m12 = vector3.Z;
    var m20 = vector.X;
    var m21 = vector.Y;
    var m22 = vector.Z;

    var num8 = m00 + m11 + m22;
    if (num8 > 0d)
    {
      var num = Math.Sqrt(num8 + 1d);
      num = 0.5d / num;
      return new Quaternion(
        (float)((m12 - m21) * num),
        (float)((m20 - m02) * num),
        (float)((m01 - m10) * num),
        (float)(num * 0.5d)
      );
    }
    if (m00 >= m11 && m00 >= m22)
    {
      var num7 = Math.Sqrt(1d + m00 - m11 - m22);
      var num4 = 0.5d / num7;
      return new Quaternion(
        (float)(0.5d * num7),
        (float)((m01 + m10) * num4),
        (float)((m02 + m20) * num4),
        (float)((m12 - m21) * num4)
      );
    }
    if (m11 > m22)
    {
      var num6 = Math.Sqrt(1d + m11 - m00 - m22);
      var num3 = 0.5d / num6;
      return new Quaternion(
        (float)((m10 + m01) * num3),
        (float)(0.5d * num6),
        (float)((m21 + m12) * num3),
        (float)((m20 - m02) * num3)
      );
    }
    var num5 = Math.Sqrt(1d + m22 - m00 - m11);
    var num2 = 0.5d / num5;
    return new Quaternion(
      (float)((m20 + m02) * num2),
      (float)((m21 + m12) * num2),
      (float)(0.5d * num5),
      (float)((m01 - m10) * num2)
    );
  }

  /// <summary>
  /// Converts this transform to the input units
  /// </summary>
  /// <param name="newUnits">The target units</param>
  /// <returns>A matrix array with the translation scaled by input units</returns>
  /// <remarks>If either the transform's <see cref="units"/> or the given <paramref name="newUnits"/> is <see langword="null"/>, will return the matrix array data unscaled</remarks>
  public double[] ConvertToUnits(string newUnits)
  {
    if (newUnits == null || units == null)
      return ToArray();

    var sf = Units.GetConversionFactor(units, newUnits);

    return new[]
    {
      matrix.M11,
      matrix.M12,
      matrix.M13,
      matrix.M14 * sf,
      matrix.M21,
      matrix.M22,
      matrix.M23,
      matrix.M24 * sf,
      matrix.M31,
      matrix.M32,
      matrix.M33,
      matrix.M34 * sf,
      matrix.M41,
      matrix.M42,
      matrix.M43,
      matrix.M44
    };
  }

  /// <summary>
  /// Returns the matrix that results from multiplying two matrices together.
  /// </summary>
  /// <param name="t1">The first transform</param>
  /// <param name="t2">The second transform</param>
  /// <returns>A transform matrix with the units of the first transform</returns>
  public static Transform operator *(Transform t1, Transform t2)
  {
    var convertedTransform = CreateMatrix(t2.ConvertToUnits(t1.units));
    var newMatrix = t1.matrix * convertedTransform;
    return new Transform(newMatrix, t1.units);
  }

  /// <summary>
  /// Returns the double array of the transform matrix
  /// </summary>
  /// <returns></returns>
  public double[] ToArray()
  {
    return new double[]
    {
      matrix.M11,
      matrix.M12,
      matrix.M13,
      matrix.M14,
      matrix.M21,
      matrix.M22,
      matrix.M23,
      matrix.M24,
      matrix.M31,
      matrix.M32,
      matrix.M33,
      matrix.M34,
      matrix.M41,
      matrix.M42,
      matrix.M43,
      matrix.M44
    };
  }

  // Creates a matrix4x4 from a double array
  internal static Matrix4x4 CreateMatrix(double[] value)
  {
    return new Matrix4x4(
      Convert.ToSingle(value[0]),
      Convert.ToSingle(value[1]),
      Convert.ToSingle(value[2]),
      Convert.ToSingle(value[3]),
      Convert.ToSingle(value[4]),
      Convert.ToSingle(value[5]),
      Convert.ToSingle(value[6]),
      Convert.ToSingle(value[7]),
      Convert.ToSingle(value[8]),
      Convert.ToSingle(value[9]),
      Convert.ToSingle(value[10]),
      Convert.ToSingle(value[11]),
      Convert.ToSingle(value[12]),
      Convert.ToSingle(value[13]),
      Convert.ToSingle(value[14]),
      Convert.ToSingle(value[15])
    );
  }

  // Creates a matrix from a float array
  internal static Matrix4x4 CreateMatrix(float[] value)
  {
    return new Matrix4x4(
      value[0],
      value[1],
      value[2],
      value[3],
      value[4],
      value[5],
      value[6],
      value[7],
      value[8],
      value[9],
      value[10],
      value[11],
      value[12],
      value[13],
      value[14],
      value[15]
    );
  }

  #region obsolete

  [JsonIgnore, Obsolete("Use the matrix property")]
  public double[] value
  {
    get => ToArray();
    set => matrix = CreateMatrix(value);
  }

  [JsonIgnore, Obsolete("Use Decompose method", true)]
  public double rotationZ
  {
    get
    {
      Decompose(out _, out Quaternion rotation, out _);
      return Math.Acos(rotation.W) * 2;
    }
  }

  /// <summary>
  /// Transform a flat list of doubles representing points
  /// </summary>
  [Obsolete("Use transform method in Point class", true)]
  public List<double> ApplyToPoints(List<double> points)
  {
    if (points.Count % 3 != 0)
      throw new SpeckleException(
        "Cannot apply transform as the points list is malformed: expected length to be multiple of 3"
      );

    var transformed = new List<double>(points.Count);
    for (var i = 0; i < points.Count; i += 3)
    {
      var point = new Point(points[i], points[i + 1], points[i + 2]);
      point.TransformTo(this, out Point transformedPoint);
      transformed.AddRange(transformedPoint.ToList());
    }
    return transformed;
  }

  /// <summary>
  /// Transform a flat list of speckle Points
  /// </summary>
  [Obsolete("Use transform method in Point class", true)]
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

  /// <summary>
  /// Transform a single speckle Point
  /// </summary>
  [Obsolete("Use transform method in Point class", true)]
  public Point ApplyToPoint(Point point)
  {
    if (point == null)
      return null;

    point.TransformTo(this, out Point transformedPoint);
    return transformedPoint;
  }

  /// <summary>
  /// Transform a list of three doubles representing a point
  /// </summary>
  [Obsolete("Use transform method in Point class")]
  public List<double> ApplyToPoint(List<double> point)
  {
    var _point = new Point(point[0], point[1], point[2]);
    _point.TransformTo(this, out Point transformedPoint);
    return transformedPoint.ToList();
  }

  /// <summary>
  /// Transform a single speckle Vector
  /// </summary>
  [Obsolete("Use transform method in Vector class")]
  public Vector ApplyToVector(Vector vector)
  {
    var newCoords = ApplyToVector(new List<double> { vector.x, vector.y, vector.z });

    return new Vector(newCoords[0], newCoords[1], newCoords[2], vector.units, vector.applicationId);
  }

  /// <summary>
  /// Transform a list of three doubles representing a vector
  /// </summary>
  [Obsolete("Use transform method in Vector class")]
  public List<double> ApplyToVector(List<double> vector)
  {
    var newPoint = new List<double>();

    for (var i = 0; i < 12; i += 4)
      newPoint.Add(vector[0] * value[i] + vector[1] * value[i + 1] + vector[2] * value[i + 2]);

    return newPoint;
  }

  /// <summary>
  /// Transform a flat list of ICurves. Note that if any of the ICurves does not implement `ITransformable`,
  /// it will not be returned.
  /// </summary>
  [Obsolete("Use transform method in Curve class")]
  public List<ICurve> ApplyToCurves(List<ICurve> curves, out bool success)
  {
    // TODO: move to curve class
    success = true;
    var transformed = new List<ICurve>();
    foreach (var curve in curves)
      if (curve is ITransformable c)
      {
        c.TransformTo(this, out ITransformable tc);
        transformed.Add((ICurve)tc);
      }
      else
      {
        success = false;
      }

    return transformed;
  }

  public static Transform Multiply(Transform left, Transform right)
  {
    throw new NotImplementedException();
  }

  #endregion
}
