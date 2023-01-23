using System;
using System.Collections.Generic;
using System.Numerics;

using Speckle.Core.Kits;
using Speckle.Newtonsoft.Json;

using Objects.Geometry;
using Vector = Objects.Geometry.Vector;
using Objects.BuiltElements.TeklaStructures;

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

    public Transform() { }

    public Transform(Matrix4x4 matrix, string units = null)
    {
      this.matrix = matrix;
      this.units = units;
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
   
  }
}
