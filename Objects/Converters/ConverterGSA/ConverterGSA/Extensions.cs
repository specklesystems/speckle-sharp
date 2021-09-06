using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Objects.Geometry;
using Speckle.GSA.API.GwaSchema;

namespace ConverterGSA
{
  public static class Extensions
  {
    #region Test Fns
    /// <summary>
    /// Test if a nullable integer has a value and is positive
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public static bool IsIndex(this int? v)
    {
      return (v.HasValue && v.Value > 0);
    }

    /// <summary>
    /// Test if a nullable double has a value and is positive
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsPositive(this double? value)
    {
      return (value.HasValue && value.Value > 0);
    }

    /// <summary>
    /// Determine if object represents a 2D element
    /// </summary>
    /// <param name="gsaEl">GsaEl object containing the element definition</param>
    /// <returns></returns>
    public static bool Is2dElement(this GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Triangle3 || gsaEl.Type == ElementType.Triangle6 || gsaEl.Type == ElementType.Quad4 || gsaEl.Type == ElementType.Quad8);
    }

    /// <summary>
    /// Determine if object represents a 3D element
    /// </summary>
    /// <param name="gsaEl">GsaEl object containing the element definition</param>
    /// <returns></returns>
    public static bool Is3dElement(this GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Brick8 || gsaEl.Type == ElementType.Pyramid5 || gsaEl.Type == ElementType.Tetra4 || gsaEl.Type == ElementType.Wedge6);
    }

    /// <summary>
    /// Determine if object represents a 1D element
    /// </summary>
    /// <param name="gsaEl">GsaEl object containing the element definition</param>
    /// <returns></returns>
    public static bool Is1dElement(this GsaEl gsaEl)
    {
      return (gsaEl.Type == ElementType.Bar || gsaEl.Type == ElementType.Beam || gsaEl.Type == ElementType.Cable || gsaEl.Type == ElementType.Damper ||
        gsaEl.Type == ElementType.Link || gsaEl.Type == ElementType.Rod || gsaEl.Type == ElementType.Spacer || gsaEl.Type == ElementType.Spring ||
        gsaEl.Type == ElementType.Strut || gsaEl.Type == ElementType.Tie);
    }

    public static bool IsGlobal(this Plane p)
    {
      return (p.origin.x == 0 && p.origin.y == 0 && p.origin.z == 0 &&
        p.xdir.x == 1 && p.xdir.y == 0 && p.xdir.z == 0 &&
        p.ydir.x == 0 && p.ydir.y == 1 && p.ydir.z == 0 &&
        p.normal.x == 0 && p.normal.y == 0 && p.normal.z == 1);
    }
    #endregion

    #region Math Fns
    /// <summary>
    /// Convert angle from degrees to radians
    /// </summary>
    /// <param name="degrees">angle in degrees</param>
    /// <returns></returns>
    public static double Radians(this double degrees)
    {
      return Math.PI * degrees / 180;
    }
    #endregion

    #region Vector Fns
    /// <summary>
    /// Returns the dot product of two vectors
    /// </summary>
    /// <param name="a">Vector 1</param>
    /// <param name="b">Vector 2</param>
    /// <returns></returns>
    public static double DotProduct(this Vector a, Vector b) => a.x * b.x + a.y * b.y + a.z * b.z;

    /// <summary>
    /// Returns a unit vector in the same direction as A
    /// </summary>
    /// <param name="a">Vector to be scaled</param>
    /// <returns></returns>
    public static Vector UnitVector(this Vector a)
    {
      var l = Norm(a);
      Vector b = new Vector()
      {
        x = a.x / l,
        y = a.y / l,
        z = a.z / l,
        units = a.units
      };
      return b;
    }

    /// <summary>
    /// Returns the length of a vector
    /// </summary>
    /// <param name="a">vector whose length is desired</param>
    /// <returns></returns>
    public static double Norm(this Vector a) => Math.Sqrt(DotProduct(a, a));

    /// <summary>
    /// Rotate vector V by an angle Theta (radians) about unit vector K using right hand rule
    /// </summary>
    /// <param name="v">vector to be rotated</param>
    /// <param name="k">unit vector defining axis of rotation</param>
    /// <param name="theta">rotation angle (radians)</param>
    /// <returns></returns>
    public static Vector Rotate(this Vector v, Vector k, double theta)
    {
      //Rodrigues' rotation formula
      //https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula

      k = k.UnitVector(); //ensure axis of rotation is a unit vector
      var v_rot1 = v * Math.Cos(theta);
      var v_rot2 = (k * v) * Math.Sin(theta);
      var v_rot3 = k * (k.DotProduct(v) * (1 - Math.Sin(theta)));

      return v_rot1 + v_rot2 + v_rot3;
    }
    #endregion

    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> source)
    {
      if (target == null)
        throw new ArgumentNullException(nameof(target));
      if (source == null)
        throw new ArgumentNullException(nameof(source));
      foreach (var element in source)
        target.Add(element);
    }
  }


}
