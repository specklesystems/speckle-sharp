using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vector = Objects.Geometry.Vector;

namespace Objects.Other
{
  /// <summary>
  /// The 4x4 transform matrix.
  /// </summary>
  /// <remarks>
  /// The 3x3 sub-matrix determines scaling.
  /// The 4th column defines translation, where the last value could be a divisor.
  /// </remarks>
  public class Transform : Base
  {
    public double[] value { get; set; } = { 1d, 0d, 0d, 0d,
                                             0d, 1d, 0d, 0d,
                                             0d, 0d, 1d, 0d,
                                             0d, 0d, 0d, 1d };

    public string units { get; set; }

    [JsonIgnore]
    public double[] translation => value.Subset(3, 7, 11, 15);

    [JsonIgnore]
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


    [JsonIgnore] public double[] scaling => value.Subset(0, 1, 2, 4, 5, 6, 8, 9, 10);

    [JsonIgnore]
    public bool isIdentity => value[0] == 1d && value[5] == 1d && value[10] == 1d && value[15] == 1d &&
                              value[1] == 0d && value[2] == 0d && value[3] == 0d &&
                              value[4] == 0d && value[6] == 0d && value[7] == 0d &&
                              value[8] == 0d && value[9] == 0d && value[11] == 0d &&
                              value[12] == 0d && value[13] == 0d && value[14] == 0d;

    [JsonIgnore] public bool isScaled => !(value[0] == 1d && value[5] == 1d && value[10] == 1d);

    public Transform()
    {
    }

    public Transform(double[] value, string units = null)
    {
      this.value = value;
      this.units = units;
    }

    /// <summary>
    /// Construct a transform given the x, y, and z bases and the translation vector
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="translation"></param>
    /// <param name="units"></param>
    public Transform(double[] x, double[] y, double[] z, double[] translation, string units = null)
    {
      this.units = units;
      value = new[]
      {
        x[ 0 ], y[ 0 ], z[ 0 ], translation[ 0 ],
        x[ 1 ], y[ 1 ], z[ 1 ], translation[ 1 ],
        x[ 2 ], y[ 2 ], z[ 2 ], translation[ 2 ],
        0d, 0d, 0d, 1d
      };
    }

    /// <summary>
    /// Get the translation, scaling, and units out of the
    /// </summary>
    /// <param name="scaling">The 3x3 sub-matrix</param>
    /// <param name="translation">The last column of the matrix (the last element being the divisor which is almost always 1)</param>
    /// <param name="units"></param>
    public void Deconstruct(out double[] scaling, out double[] translation, out string units)
    {
      scaling = this.scaling;
      translation = this.translation;
      units = this.units;
    }

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


    /// <summary>
    /// Transform a single speckle Point
    /// </summary>
    public Point ApplyToPoint(Point point)
    {
      var (x, y, z, units) = point;
      var newCoords = ApplyToPoint(new List<double> { x, y, z });
      return new Point(newCoords[0], newCoords[1], newCoords[2], point.units, point.applicationId);
    }

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

    /// <summary>
    /// Transform a single speckle Vector
    /// </summary>
    public Vector ApplyToVector(Vector vector)
    {
      var newCoords = ApplyToVector(new List<double> { vector.x, vector.y, vector.z });

      return new Geometry.Vector(newCoords[0], newCoords[1], newCoords[2], vector.units, vector.applicationId);
    }

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

    /// <summary>
    /// Multiplies two transform matrices together
    /// </summary>
    /// <param name="t1">The first source transform</param>
    /// <param name="t2">The second source transform</param>
    /// <returns></returns>
    public static Transform operator *(Transform t1, Transform t2)
    {
      var result = new double[16];
      var row = 0;
      for (var i = 0; i < 16; i += 4)
      {
        for (var j = 0; j < 4; j++)
        {
          result[i + j] = t1.value[i] * t2.value[j] +
                            t1.value[i + 1] * t2.value[j + 4] +
                            t1.value[i + 2] * t2.value[j + 8] +
                            t1.value[i + 3] * t2.value[j + 12];
        }
      }

      return new Transform(result);
    }
  }

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