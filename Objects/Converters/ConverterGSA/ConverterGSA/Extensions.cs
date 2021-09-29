using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Objects.Geometry;
using Objects.Structural;
using Objects.Structural.Geometry;
using Objects.Structural.GSA.Geometry;
using Objects.Structural.Loading;
using Speckle.GSA.API.GwaSchema;
using GwaMemberType = Speckle.GSA.API.GwaSchema.MemberType;
using GwaAxisDirection3 = Speckle.GSA.API.GwaSchema.AxisDirection3;
using GwaAxisDirection6 = Speckle.GSA.API.GwaSchema.AxisDirection6;
using AxisDirection6 = Objects.Structural.GSA.Geometry.AxisDirection6;
using PathType = Objects.Structural.GSA.Bridge.PathType;
using GwaPathType = Speckle.GSA.API.GwaSchema.PathType;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Objects.Structural.GSA.Bridge;
using Objects.Structural.GSA.Loading;

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

    public static bool Is1dMember(this GsaMemb gsaMemb)
    {
      return (gsaMemb.Type == GwaMemberType.Beam || gsaMemb.Type == GwaMemberType.Column || gsaMemb.Type == GwaMemberType.Generic1d || gsaMemb.Type == GwaMemberType.Void1d);
    }

    public static bool Is2dMember(this GsaMemb gsaMemb)
    {
      return (gsaMemb.Type == GwaMemberType.Generic2d || gsaMemb.Type == GwaMemberType.Slab || gsaMemb.Type == GwaMemberType.Void2d || gsaMemb.Type == GwaMemberType.Wall);
    }

    public static bool Is3dMember(this GsaMemb gsaMemb)
    {
      return (gsaMemb.Type == GwaMemberType.Generic3d);
    }

    public static bool IsGlobal(this Plane p)
    {
      return (p.origin.x == 0 && p.origin.y == 0 && p.origin.z == 0 &&
        p.xdir.x == 1 && p.xdir.y == 0 && p.xdir.z == 0 &&
        p.ydir.x == 0 && p.ydir.y == 1 && p.ydir.z == 0 &&
        p.normal.x == 0 && p.normal.y == 0 && p.normal.z == 1);
    }

    public static bool IsXElevation(this Plane p)
    {
      return (p.origin.x == 0 && p.origin.y == 0 && p.origin.z == 0 &&
        p.xdir.x == 0 && p.xdir.y == -1 && p.xdir.z == 0 &&
        p.ydir.x == 0 && p.ydir.y == 0 && p.ydir.z == 1 &&
        p.normal.x == -1 && p.normal.y == 0 && p.normal.z == 0);
    }

    public static bool IsYElevation(this Plane p)
    {
      return (p.origin.x == 0 && p.origin.y == 0 && p.origin.z == 0 &&
        p.xdir.x == 1 && p.xdir.y == 0 && p.xdir.z == 0 &&
        p.ydir.x == 0 && p.ydir.y == 0 && p.ydir.z == 1 &&
        p.normal.x == 0 && p.normal.y == -1 && p.normal.z == 0);
    }

    public static bool IsVertical(this Plane p)
    {
      return (p.origin.x == 0 && p.origin.y == 0 && p.origin.z == 0 &&
        p.xdir.x == 0 && p.xdir.y == 0 && p.xdir.z == 1 &&
        p.ydir.x == 1 && p.ydir.y == 0 && p.ydir.z == 0 &&
        p.normal.x == 0 && p.normal.y == 1 && p.normal.z == 0);
    }
    #endregion

    #region Enum conversions
    #region ToSpeckle
    public static ElementType1D ToSpeckle1d(this ElementType gsaType)
    {
      switch (gsaType)
      {
        case ElementType.Bar: return ElementType1D.Bar;
        case ElementType.Cable: return ElementType1D.Cable;
        case ElementType.Damper: return ElementType1D.Damper;
        case ElementType.Link: return ElementType1D.Link;
        case ElementType.Rod: return ElementType1D.Rod;
        case ElementType.Spacer: return ElementType1D.Spacer;
        case ElementType.Spring: return ElementType1D.Spring;
        case ElementType.Strut: return ElementType1D.Strut;
        case ElementType.Tie: return ElementType1D.Tie;
        default: return ElementType1D.Beam;
      }
    }
    public static ElementType2D ToSpeckle2d(this ElementType gsaType)
    {
      switch (gsaType)
      {
        case ElementType.Triangle3: return ElementType2D.Triangle3;
        case ElementType.Triangle6: return ElementType2D.Triangle6;
        case ElementType.Quad8: return ElementType2D.Quad8;
        default: return ElementType2D.Quad4;
      }
    }

    public static ElementType1D ToSpeckle1d(this GwaMemberType gsaMemberType)
    {
      switch (gsaMemberType)
      {
        case GwaMemberType.Beam:
        case GwaMemberType.Column:
        case GwaMemberType.Generic1d:
        case GwaMemberType.Void1d:
          return ElementType1D.Beam;
        default:
          throw new Exception(gsaMemberType.ToString() + " is not a valid 1D member type.");
      }
    }

    public static ElementType2D ToSpeckle2d(this GwaMemberType gsaMemberType)
    {
      switch(gsaMemberType)
      {
        case GwaMemberType.Generic2d:
        case GwaMemberType.Slab:
        case GwaMemberType.Void2d:
        case GwaMemberType.Wall:
          return ElementType2D.Quad4;
        default:
          throw new Exception(gsaMemberType.ToString() + " is not a valid 2D member type.");
      }
    }

    public static GridSurfaceSpanType ToSpeckle(this GridSurfaceSpan gsaGridSurfaceSpan)
    {
      switch (gsaGridSurfaceSpan)
      {
        case GridSurfaceSpan.One: return GridSurfaceSpanType.OneWay;
        case GridSurfaceSpan.Two: return GridSurfaceSpanType.TwoWay;
        default: return GridSurfaceSpanType.NotSet;
      }
    }

    public static LoadExpansion ToSpeckle(this GridExpansion gsaExpansion)
    {
      switch(gsaExpansion)
      {
        case GridExpansion.Legacy: return LoadExpansion.Legacy;
        case GridExpansion.PlaneAspect: return LoadExpansion.PlaneAspect;
        case GridExpansion.PlaneCorner: return LoadExpansion.PlaneCorner;
        case GridExpansion.PlaneSmooth: return LoadExpansion.PlaneSmooth;
        default: return LoadExpansion.NotSet;
      }
    }

    public static LoadType ToSpeckle(this StructuralLoadCaseType gsaLoadType)
    {
      switch (gsaLoadType)
      {
        case StructuralLoadCaseType.Dead: return LoadType.Dead;
        case StructuralLoadCaseType.Earthquake: return LoadType.SeismicStatic;
        case StructuralLoadCaseType.Live: return LoadType.Live;
        case StructuralLoadCaseType.Rain: return LoadType.Rain;
        case StructuralLoadCaseType.Snow: return LoadType.Snow;
        case StructuralLoadCaseType.Soil: return LoadType.Soil;
        case StructuralLoadCaseType.Thermal: return LoadType.Thermal;
        case StructuralLoadCaseType.Wind: return LoadType.Wind;
        default: return LoadType.None;
      }
    }

    public static ActionType GetActionType(this StructuralLoadCaseType gsaLoadType)
    {
      switch (gsaLoadType)
      {
        case StructuralLoadCaseType.Dead:
        case StructuralLoadCaseType.Soil:
          return ActionType.Permanent;
        case StructuralLoadCaseType.Live:
        case StructuralLoadCaseType.Wind:
        case StructuralLoadCaseType.Snow:
        case StructuralLoadCaseType.Rain:
        case StructuralLoadCaseType.Thermal:
          return ActionType.Variable;
        case StructuralLoadCaseType.Earthquake: //TODO: variable? accidental? something else
          return ActionType.Accidental;
        default:
          //StructuralLoadCaseType.NotSet
          //StructuralLoadCaseType.Generic
          return ActionType.None;
      }
    }

    public static FaceLoadType ToSpeckle(this Load2dFaceType gsaType)
    {
      switch (gsaType)
      {
        case Load2dFaceType.General: return FaceLoadType.Variable;
        case Load2dFaceType.Point: return FaceLoadType.Point;
        default: return FaceLoadType.Constant;
      }
    }

    public static Thermal2dLoadType ToSpeckle(this Load2dThermalType gsaType)
    {
      switch (gsaType)
      {
        case Load2dThermalType.Uniform: return Thermal2dLoadType.Uniform;
        case Load2dThermalType.Gradient: return Thermal2dLoadType.Gradient;
        case Load2dThermalType.General: return Thermal2dLoadType.General;
        default: return Thermal2dLoadType.NotSet;
      }
    }

    public static LoadDirection2D ToSpeckle(this GwaAxisDirection3 gsaDirection)
    {
      switch (gsaDirection)
      {
        case GwaAxisDirection3.X: return LoadDirection2D.X;
        case GwaAxisDirection3.Y: return LoadDirection2D.Y;
        case GwaAxisDirection3.Z: return LoadDirection2D.Z;
        default: return LoadDirection2D.Z; //TODO: handle NotSet case. Throw exception? Add to LoadDirection2D enum?
      }
    }

    public static LoadDirection ToSpeckleLoad(this GwaAxisDirection6 gsaDirection)
    {
      switch (gsaDirection)
      {
        case GwaAxisDirection6.X: return LoadDirection.X;
        case GwaAxisDirection6.Y: return LoadDirection.Y;
        case GwaAxisDirection6.Z: return LoadDirection.Z;
        case GwaAxisDirection6.XX: return LoadDirection.XX;
        case GwaAxisDirection6.YY: return LoadDirection.YY;
        case GwaAxisDirection6.ZZ: return LoadDirection.ZZ;
        default: throw new Exception(gsaDirection + " can not be converted into LoadDirection enum");        
      }
    }

    public static LoadAxisType ToSpeckle(this AxisRefType gsaType)
    {
      //TO DO: update when there are more options for LoadAxisType
      switch (gsaType)
      {
        case AxisRefType.Local:
          return LoadAxisType.Local;
        case AxisRefType.Reference:
        case AxisRefType.NotSet:
        case AxisRefType.Global:
        default:
          return LoadAxisType.Global;
      }
    }

    public static LoadAxisType ToSpeckle(this LoadBeamAxisRefType gsaType)
    {
      //TO DO: update when there are more options for LoadAxisType
      switch (gsaType)
      {
        case LoadBeamAxisRefType.Local:
          return LoadAxisType.Local;
        case LoadBeamAxisRefType.Reference:
        case LoadBeamAxisRefType.Natural:
        case LoadBeamAxisRefType.NotSet:
        case LoadBeamAxisRefType.Global:
        default:
          return LoadAxisType.Global;
      }
    }

    public static BeamLoadType ToSpeckle(this Type t)
    {
      if (t == typeof(GsaLoadBeamPoint))
      {
        return BeamLoadType.Point;
      }
      else if (t == typeof(GsaLoadBeamLine))
      {
        return BeamLoadType.Linear;
      }
      else if (t == typeof(GsaLoadBeamPatch))
      {
        return BeamLoadType.Patch;
      }
      else if (t == typeof(GsaLoadBeamTrilin))
      {
        return BeamLoadType.TriLinear;
      }
      else
      {
        return BeamLoadType.Uniform;
      }
    }

    public static BaseReferencePoint ToSpeckle(this ReferencePoint gsaReferencePoint)
    {
      switch (gsaReferencePoint)
      {
        case ReferencePoint.BottomCentre: return BaseReferencePoint.BotCentre;
        case ReferencePoint.BottomLeft: return BaseReferencePoint.BotLeft;
        default: return BaseReferencePoint.Centroid;
      }
    }

    public static ReferenceSurface ToSpeckle(this Property2dRefSurface gsaRefPt)
    {
      switch (gsaRefPt)
      {
        case Property2dRefSurface.BottomCentre: return ReferenceSurface.Bottom;
        case Property2dRefSurface.TopCentre: return ReferenceSurface.Top;
        default: return ReferenceSurface.Middle;
      }
    }

    public static LinkageType ToSpeckle(this RigidConstraintType gsaType)
    {
      switch (gsaType)
      {
        case RigidConstraintType.ALL: return LinkageType.ALL;
        case RigidConstraintType.XY_PLANE: return LinkageType.XY_PLANE;
        case RigidConstraintType.YZ_PLANE: return LinkageType.YZ_PLANE;
        case RigidConstraintType.ZX_PLANE: return LinkageType.ZX_PLANE;
        case RigidConstraintType.XY_PLATE: return LinkageType.XY_PLATE;
        case RigidConstraintType.YZ_PLATE: return LinkageType.YZ_PLATE;
        case RigidConstraintType.ZX_PLATE: return LinkageType.ZX_PLATE;
        case RigidConstraintType.PIN: return LinkageType.PIN;
        case RigidConstraintType.XY_PLANE_PIN: return LinkageType.XY_PLANE_PIN;
        case RigidConstraintType.YZ_PLANE_PIN: return LinkageType.YZ_PLANE_PIN;
        case RigidConstraintType.ZX_PLANE_PIN: return LinkageType.ZX_PLANE_PIN;
        case RigidConstraintType.XY_PLATE_PIN: return LinkageType.XY_PLATE_PIN;
        case RigidConstraintType.YZ_PLATE_PIN: return LinkageType.YZ_PLATE_PIN;
        case RigidConstraintType.ZX_PLATE_PIN: return LinkageType.ZX_PLATE_PIN;
        case RigidConstraintType.Custom: return LinkageType.Custom;
        default: return LinkageType.NotSet;
      }
    }

    public static AxisDirection6 ToSpeckle(this GwaAxisDirection6 gsa)
    {
      switch (gsa)
      {
        case GwaAxisDirection6.X: return AxisDirection6.X;
        case GwaAxisDirection6.Y: return AxisDirection6.Y;
        case GwaAxisDirection6.Z: return AxisDirection6.Z;
        case GwaAxisDirection6.XX: return AxisDirection6.XX;
        case GwaAxisDirection6.YY: return AxisDirection6.YY;
        case GwaAxisDirection6.ZZ: return AxisDirection6.ZZ;
        default: return AxisDirection6.NotSet;
      }
    }

    public static InfluenceType ToSpeckle(this InfType gsaType)
    {
      switch (gsaType)
      {
        case InfType.DISP: return InfluenceType.DISPLACEMENT;
        case InfType.FORCE: return InfluenceType.FORCE;
        default: return InfluenceType.NotSet;
      }
    }

    public static PathType ToSpeckle(this GwaPathType gsaType)
    {
      switch (gsaType)
      { 
        case GwaPathType.LANE: return PathType.LANE;
        case GwaPathType.FOOTWAY: return PathType.FOOTWAY;
        case GwaPathType.TRACK: return PathType.TRACK;
        case GwaPathType.VEHICLE: return PathType.VEHICLE;
        case GwaPathType.CWAY_1WAY: return PathType.CWAY_1WAY;
        case GwaPathType.CWAY_2WAY: return PathType.CWAY_2WAY;
        default: return PathType.NotSet;
      }
    }
    #endregion

    #region ToNative
    public static ElementType ToNative(this ElementType1D speckleType)
    {
      switch (speckleType)
      {
        case ElementType1D.Bar:
          return ElementType.Bar;
        case ElementType1D.Cable:
          return ElementType.Cable;
        case ElementType1D.Damper:
          return ElementType.Damper;
        case ElementType1D.Link:
          return ElementType.Link;
        case ElementType1D.Rod:
          return ElementType.Rod;
        case ElementType1D.Spacer:
          return ElementType.Spacer;
        case ElementType1D.Spring:
          return ElementType.Spring;
        case ElementType1D.Strut:
          return ElementType.Strut;
        case ElementType1D.Tie:
          return ElementType.Tie;
        default:
          return ElementType.Beam;
      }
    }

    public static ElementType ToNative(this ElementType2D speckleType)
    {
      switch (speckleType)
      {
        case ElementType2D.Triangle3:
          return ElementType.Triangle3;
        case ElementType2D.Triangle6:
          return ElementType.Triangle6;
        case ElementType2D.Quad8:
          return ElementType.Quad8;
        case ElementType2D.Quad4:
        default:
          return ElementType.Quad4;
      }
    }
    #endregion
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

    public static Colour ColourToNative(this string speckleColour)
    {
      return Enum.TryParse(speckleColour, out Colour gsaColour) ? gsaColour : Colour.NO_RGB;
    }

    public static ReleaseCode ReleaseCodeToNative(this char speckleRelease)
    {
      switch (speckleRelease)
      {
        case 'R':
          return ReleaseCode.Released;
        case 'F':
          return ReleaseCode.Fixed;
        case 'K':
          return ReleaseCode.Stiff;
        default:
          return ReleaseCode.NotSet;
      }
    }

    public static Dictionary<GwaAxisDirection6, ReleaseCode> ReleasesToNative(this string speckleCode)
    {
      Dictionary<GwaAxisDirection6, ReleaseCode> gsaReleases = null;
      if (speckleCode.Length == 6)
      {
        gsaReleases = new Dictionary<GwaAxisDirection6, ReleaseCode>()
        {
          { GwaAxisDirection6.X, speckleCode[0].ReleaseCodeToNative() },
          { GwaAxisDirection6.Y, speckleCode[1].ReleaseCodeToNative() },
          { GwaAxisDirection6.Z, speckleCode[2].ReleaseCodeToNative() },
          { GwaAxisDirection6.XX, speckleCode[3].ReleaseCodeToNative() },
          { GwaAxisDirection6.YY, speckleCode[4].ReleaseCodeToNative() },
          { GwaAxisDirection6.ZZ, speckleCode[5].ReleaseCodeToNative() }
        };
      }
      return gsaReleases;
    }

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
