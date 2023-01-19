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
  /// Generic transform class
  /// </summary>
  public class Transform
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

    [JsonIgnore] public Vector3 translation => matrix.Translation;

    [JsonIgnore]
    public Quaternion rotation
    {
      get
      {
        if (Matrix4x4.Decompose(matrix, out Vector3 _scale, out Quaternion _rotation, out Vector3 _translation))
        {
          return _rotation;
        }
        else
        {
          return Quaternion.Identity;
        }
      }
    }

    [JsonIgnore]
    public Vector3 scale
    {
      get
      {
        Vector3 scale;
        scale.X = new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41).Length();
        scale.Y = new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42).Length();
        scale.Z = new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43).Length();
        if (matrix.GetDeterminant() < 0) // indicates negative scale
        {
          scale.X *= -1;
        }
        return scale;
      }
    }

    [JsonIgnore] public bool isIdentity => matrix.IsIdentity;

    public Transform() { }

    public Transform(Matrix4x4 matrix, string units = null)
    {
      this.matrix = matrix;
      this.units = units;
    }

    /// <summary>
    /// Multiplies two transform matrices together. Assumes they have the same units.
    /// </summary>
    /// <param name="t1">The first source transform</param>
    /// <param name="t2">The second source transform</param>
    /// <returns></returns>
    public static Transform operator *(Transform t1, Transform t2)
    {
      var newMatrix = t1.matrix * t2.matrix;
      return new Transform(newMatrix);
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
  }
}
