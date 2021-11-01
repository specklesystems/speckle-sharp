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
using MemberType = Objects.Structural.Geometry.MemberType;
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
using Speckle.GSA.API;
using Speckle.Core.Models;
using Objects.Structural.Properties;
using Objects.Structural.Materials;

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

    public static bool HasValues(this List<int> v)
    {
      return (v != null && v.Count > 0);
    }

    public static bool HasValues(this List<List<int>> v)
    {
      return (v != null && v.Count > 0);
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

    public static MemberType ToSpeckle(this Section1dType gsaType)
    {
      switch (gsaType)
      {
        case Section1dType.Beam:
        case Section1dType.CompositeBeam: 
          return MemberType.Beam;
        case Section1dType.Column:
          return MemberType.Column;
        case Section1dType.Slab: 
        case Section1dType.RibbedSlab:
          return MemberType.Slab;
        case Section1dType.Pile:
        case Section1dType.Explicit:
        case Section1dType.Generic:
        default:
          return MemberType.Generic1D;
      }
    }

    public static Section1dType ToNative(this Objects.Structural.Geometry.MemberType speckleElementType)
    {
      switch (speckleElementType)
      {
        case Objects.Structural.Geometry.MemberType.Beam: return Section1dType.Beam;
        case Objects.Structural.Geometry.MemberType.Column: return Section1dType.Column;
        default: return Section1dType.Generic;
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

    public static PropertyType2D ToSpeckle(this Property2dType gsaType)
    {
      switch(gsaType)
      {
        case Property2dType.Curved: return PropertyType2D.Curved;
        case Property2dType.Fabric: return PropertyType2D.Fabric;
        case Property2dType.Load: return PropertyType2D.Load;
        case Property2dType.Plate: return PropertyType2D.Plate;
        case Property2dType.Shell: return PropertyType2D.Shell;
        case Property2dType.Stress: return PropertyType2D.Stress;
        default: throw new Exception(gsaType.ToString() + " can not be converted to a valid speckle 2D property type.");
      }
    }

    public static Property2dType ToNative(this PropertyType2D propertyType)
    {
      switch (propertyType)
      {
        case PropertyType2D.Curved : return Property2dType.Curved;
        case PropertyType2D.Fabric : return Property2dType.Fabric;
        case PropertyType2D.Load:   return Property2dType.Load; 
        case PropertyType2D.Plate:  return Property2dType.Plate;
        case PropertyType2D.Shell:  return Property2dType.Shell;
        case PropertyType2D.Stress : return Property2dType.Stress;
        default: throw new Exception(propertyType.ToString() + " can not be converted to a valid native 2D property type.");
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

    public static GridSurfaceSpan ToNative(this GridSurfaceSpanType gsaGridSurfaceSpan)
    {
      switch (gsaGridSurfaceSpan)
      {
        case GridSurfaceSpanType.OneWay: return GridSurfaceSpan.One;
        case GridSurfaceSpanType.TwoWay: return GridSurfaceSpan.Two;
        default: return GridSurfaceSpan.NotSet;
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

    public static GridExpansion ToNative(this LoadExpansion gsaExpansion)
    {
      switch (gsaExpansion)
      {
        case LoadExpansion.Legacy:      return GridExpansion.Legacy;
        case LoadExpansion.PlaneAspect: return GridExpansion.PlaneAspect;
        case LoadExpansion.PlaneCorner: return GridExpansion.PlaneCorner;
        case LoadExpansion.PlaneSmooth: return GridExpansion.PlaneSmooth;
        default: return GridExpansion.NotSet;
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
    
    public static GwaAxisDirection6 ToNative(this LoadDirection loadDirection)
    {
      switch (loadDirection)
      {
        case LoadDirection.X: return GwaAxisDirection6.X;
        case LoadDirection.Y: return GwaAxisDirection6.Y;
        case LoadDirection.Z: return GwaAxisDirection6.Z;
        case LoadDirection.XX: return GwaAxisDirection6.XX;
        case LoadDirection.YY: return GwaAxisDirection6.YY;
        case LoadDirection.ZZ: return GwaAxisDirection6.ZZ;
        default: throw new Exception(loadDirection + " can not be converted into GwaAxisDirection6 enum");        
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

    public static ReferencePoint ToNative(this BaseReferencePoint baseReferencePoint)
    {
      switch (baseReferencePoint)
      {
        case BaseReferencePoint.BotCentre: return ReferencePoint.BottomCentre;
        case BaseReferencePoint.BotLeft: return ReferencePoint.BottomLeft;
        case BaseReferencePoint.BotRight: return ReferencePoint.BottomRight;
        case BaseReferencePoint.Centroid: return ReferencePoint.Centroid;
        case BaseReferencePoint.MidLeft: return ReferencePoint.MiddleLeft;
        case BaseReferencePoint.MidRight: return ReferencePoint.MiddleRight;
        case BaseReferencePoint.TopCentre: return ReferencePoint.TopCentre;
        case BaseReferencePoint.TopLeft: return ReferencePoint.TopLeft;
        case BaseReferencePoint.TopRight: return ReferencePoint.TopRight;
        default: return ReferencePoint.Centroid;
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

    public static Property2dRefSurface ToNative(this ReferenceSurface refSurface)
    {
      switch (refSurface)
      {
        case ReferenceSurface.Bottom: return Property2dRefSurface.BottomCentre;
        case ReferenceSurface.Top: return Property2dRefSurface.TopCentre;
        default: return Property2dRefSurface.Centroid;
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

    public static InfType ToNative(this InfluenceType gsaType)
    {
      switch (gsaType)
      {
        case InfluenceType.DISPLACEMENT: return InfType.DISP;
        case InfluenceType.FORCE: return InfType.FORCE;
        default: return InfType.NotSet;
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
    
    public static GwaPathType ToNative(this PathType gsaType)
    {
      switch (gsaType)
      { 
        case PathType.LANE: return GwaPathType.LANE;
        case PathType.FOOTWAY: return GwaPathType.FOOTWAY;
        case PathType.TRACK: return GwaPathType.TRACK;
        case PathType.VEHICLE: return GwaPathType.VEHICLE;
        case PathType.CWAY_1WAY: return GwaPathType.CWAY_1WAY;
        case PathType.CWAY_2WAY: return GwaPathType.CWAY_2WAY;
        default: return GwaPathType.NotSet;
      }
    }
    #endregion

    #region ToNative
    public static ElementType ToNative(this ElementType1D speckleType)
    {
      switch (speckleType)
      {
        case ElementType1D.Beam: return ElementType.Beam;
        case ElementType1D.Bar: return ElementType.Bar;
        case ElementType1D.Cable: return ElementType.Cable;
        case ElementType1D.Damper: return ElementType.Damper;
        case ElementType1D.Link: return ElementType.Link;
        case ElementType1D.Rod: return ElementType.Rod;
        case ElementType1D.Spacer: return ElementType.Spacer;
        case ElementType1D.Spring: return ElementType.Spring;
        case ElementType1D.Strut: return ElementType.Strut;
        case ElementType1D.Tie: return ElementType.Tie;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum"); 
      }
    }

    public static GwaMemberType ToNativeMember(this ElementType1D speckleType)
    {
      switch (speckleType)
      {
        case ElementType1D.Beam:
        case ElementType1D.Bar:
        case ElementType1D.Cable:
        case ElementType1D.Damper:
        case ElementType1D.Link:
        case ElementType1D.Rod:
        case ElementType1D.Spacer:
        case ElementType1D.Spring:
        case ElementType1D.Strut: 
        case ElementType1D.Tie: 
          return GwaMemberType.Generic1d;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static ElementType ToNative(this ElementType2D speckleType)
    {
      switch (speckleType)
      {
        case ElementType2D.Triangle3: return ElementType.Triangle3;
        case ElementType2D.Triangle6: return ElementType.Triangle6;
        case ElementType2D.Quad8: return ElementType.Quad8;
        case ElementType2D.Quad4: return ElementType.Quad4;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static GwaMemberType ToNativeMember(this ElementType2D speckleType)
    {
      switch (speckleType)
      {
        case ElementType2D.Triangle3:
        case ElementType2D.Triangle6:
        case ElementType2D.Quad4:
        case ElementType2D.Quad8:
          return GwaMemberType.Generic2d;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static Load2dFaceType ToNative(this FaceLoadType speckleType)
    {
      switch (speckleType)
      {
        case FaceLoadType.Constant: return Load2dFaceType.Uniform;
        case FaceLoadType.Point: return Load2dFaceType.Point;
        case FaceLoadType.Variable: return Load2dFaceType.General;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static GwaAxisDirection3 ToNative(this LoadDirection2D speckleType)
    {
      switch (speckleType)
      {
        case LoadDirection2D.X: return GwaAxisDirection3.X;
        case LoadDirection2D.Y: return GwaAxisDirection3.Y;
        case LoadDirection2D.Z: return GwaAxisDirection3.Z;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static AxisRefType ToNative(this LoadAxisType speckleType)
    {
      switch (speckleType)
      {
        case LoadAxisType.Global: return AxisRefType.Global;
        case LoadAxisType.Local: return AxisRefType.Local;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static LoadBeamAxisRefType ToNativeBeamAxisRefType(this LoadAxisType speckleType)
    {
      switch (speckleType)
      {
        case LoadAxisType.Global: return LoadBeamAxisRefType.Global;
        case LoadAxisType.Local: return LoadBeamAxisRefType.Local;
        case LoadAxisType.DeformedLocal:  return LoadBeamAxisRefType.Local;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static LoadCategory LoadCategoryToNative(this string category)
    {
      switch(category.ToLowerInvariant())
      {
        case "residential": return LoadCategory.Residential;
        case "office": return LoadCategory.Office;
        case "congregationarea": return LoadCategory.CongregationArea;
        case "shop": return LoadCategory.Shop;
        case "storage": return LoadCategory.Storage;
        case "lighttraffic": return LoadCategory.LightTraffic;
        case "traffic": return LoadCategory.Traffic;
        case "roofs": return LoadCategory.Roofs;
        case "notset": return LoadCategory.NotSet;
        default: throw new Exception(category + " speckle string can not be converted into native enum");
      }
    }

    public static IncludeOption IncludeOptionToNative(this string include)
    {
      switch(include.ToLowerInvariant())
      {
        case "undefined": return IncludeOption.Undefined;
        case "unfavourable": return IncludeOption.Unfavourable;
        case "favourable": return IncludeOption.Favourable;
        case "both": return IncludeOption.Both;
        default: throw new Exception(include + " speckle string can not be converted into native enum");
      }
    }

    public static StructuralLoadCaseType ToNative(this LoadType speckleType)
    {
      switch (speckleType)
      {
        case LoadType.Dead: return StructuralLoadCaseType.Dead;
        case LoadType.SeismicStatic: return StructuralLoadCaseType.Earthquake;
        case LoadType.Live: return StructuralLoadCaseType.Live;
        case LoadType.Rain: return StructuralLoadCaseType.Rain;
        case LoadType.Snow: return StructuralLoadCaseType.Snow;
        case LoadType.Soil: return StructuralLoadCaseType.Soil;
        case LoadType.Thermal: return StructuralLoadCaseType.Thermal;
        case LoadType.Wind: return StructuralLoadCaseType.Wind;
        case LoadType.None: return StructuralLoadCaseType.NotSet;
        default: return StructuralLoadCaseType.Generic;
      }
    }

    public static Load2dThermalType ToNative(this Thermal2dLoadType speckleType)
    {
      switch (speckleType)
      {
        case Thermal2dLoadType.Uniform: return Load2dThermalType.Uniform;
        case Thermal2dLoadType.Gradient: return Load2dThermalType.Gradient;
        case Thermal2dLoadType.General: return Load2dThermalType.General;
        default: throw new Exception(speckleType.ToString() + " speckle enum can not be converted into native enum");
      }
    }

    public static GwaAxisDirection6 ToNative(this AxisDirection6 speckleType)
    {
      switch (speckleType)
      {
        case AxisDirection6.X: return GwaAxisDirection6.X;
        case AxisDirection6.Y: return GwaAxisDirection6.Y;
        case AxisDirection6.Z: return GwaAxisDirection6.Z;
        case AxisDirection6.XX: return GwaAxisDirection6.XX;
        case AxisDirection6.YY: return GwaAxisDirection6.YY;
        case AxisDirection6.ZZ: return GwaAxisDirection6.ZZ;
        default: return GwaAxisDirection6.NotSet;
      }
    }

    public static RigidConstraintType ToNative(this LinkageType speckleType)
    {
      switch (speckleType)
      {
        case LinkageType.ALL: return RigidConstraintType.ALL;
        case LinkageType.XY_PLANE: return RigidConstraintType.XY_PLANE;
        case LinkageType.YZ_PLANE: return RigidConstraintType.YZ_PLANE;
        case LinkageType.ZX_PLANE: return RigidConstraintType.ZX_PLANE;
        case LinkageType.XY_PLATE: return RigidConstraintType.XY_PLATE;
        case LinkageType.YZ_PLATE: return RigidConstraintType.YZ_PLATE;
        case LinkageType.ZX_PLATE: return RigidConstraintType.ZX_PLATE;
        case LinkageType.PIN: return RigidConstraintType.PIN;
        case LinkageType.XY_PLANE_PIN: return RigidConstraintType.XY_PLANE_PIN;
        case LinkageType.YZ_PLANE_PIN: return RigidConstraintType.YZ_PLANE_PIN;
        case LinkageType.ZX_PLANE_PIN: return RigidConstraintType.ZX_PLANE_PIN;
        case LinkageType.XY_PLATE_PIN: return RigidConstraintType.XY_PLATE_PIN;
        case LinkageType.YZ_PLATE_PIN: return RigidConstraintType.YZ_PLATE_PIN;
        case LinkageType.ZX_PLATE_PIN: return RigidConstraintType.ZX_PLATE_PIN;
        case LinkageType.Custom: return RigidConstraintType.Custom;
        default: return RigidConstraintType.NotSet;
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
    public static double Degrees(this double radians)
    {
      double degrees = (180 / Math.PI) * radians;
      return (degrees);
    }
    #endregion

    #region Geometric Fns
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

    public static bool Equals(this Point p1, Point p2, int decimalPlaces)
    {
      if (p1 == null && p2 == null)
      {
        return true;
      }
      else if (p1 == null || p2 == null)
      {
        return false;
      }
      double margin = 1;
      for (int i = 0; i < decimalPlaces; i++)
      {
        margin *= 0.1;
      }
      return ((Math.Abs(p1.x - p2.x) < margin) && (Math.Abs(p1.y - p2.y) < margin) && (Math.Abs(p1.z - p2.z) < margin));
    }

    public static bool Equals(this Vector v1, Vector v2, int decimalPlaces)
    {
      if (v1 == null && v2 == null)
      {
        return true;
      }
      else if (v1 == null || v2 == null)
      {
        return false;
      }
      double margin = 1;
      for (int i = 0; i < decimalPlaces; i++)
      {
        margin *= 0.1;
      }
      return ((Math.Abs(v1.x - v2.x) < margin) && (Math.Abs(v1.y - v2.y) < margin) && (Math.Abs(v1.z - v2.z) < margin));
    }

    #endregion

    public static string ToGwaString(this Polyline specklePolyline)
    {
      var is3d = specklePolyline.Is3d();

      //create string
      var str = "";
      for (var i = 0; i < specklePolyline.value.Count(); i += 3)
      {
        str += "(" + specklePolyline.value[i] + "," + specklePolyline.value[i + 1];
        if (is3d) str += "," + specklePolyline.value[i + 2];
        str += ") ";
      }
      str = str.Remove(str.Length - 1, 1) + "(m)"; //TODO: add units to end of string
      return str;
    }

    public static bool Is3d(this Polyline specklePolyline)
    {
      for (var i = 0; i < specklePolyline.value.Count(); i+= 3)
      {
        if (specklePolyline.value[i + 2] != 0) return true;
      }
      return false;
    }

    public static List<double> GetValues(this Polyline specklePolyline)
    {
      if (specklePolyline.Is3d())
      {
        return specklePolyline.value;
      }
      else
      {
        var v = new List<double>();
        for (var i = 0; i < specklePolyline.value.Count(); i += 3)
        {
          v.Add(specklePolyline.value[i]);
          v.Add(specklePolyline.value[i + 1]);
        }
        return v;
      }
    }

    #region ResolveIndices
    public static List<int> GetIndicies<T>(this List<Base> speckleObject)
    {
      if (speckleObject == null) return null;
      var gsaIndices = new List<int>();
      foreach (var o in speckleObject)
      {
        var index = Instance.GsaModel.Cache.LookupIndex<T>(o.applicationId);
        if (index.HasValue) gsaIndices.Add(index.Value);
      }
      return (gsaIndices.Count() > 0) ? gsaIndices : null;
    }
    public static List<int> GetIndicies(this List<Node> speckleObject)
    {
      if (speckleObject == null) return null;
      var gsaIndices = new List<int>();
      foreach (var o in speckleObject)
      {
        var index = Instance.GsaModel.Cache.LookupIndex<GsaNode>(o.applicationId);
        if (index.HasValue) gsaIndices.Add(index.Value);
      }
      return (gsaIndices.Count() > 0) ? gsaIndices : null;
    }

    public static int? GetIndex<T>(this Base speckleObject) => (speckleObject == null) ? null : (int?)Instance.GsaModel.Cache.ResolveIndex<T>(speckleObject.applicationId);
    #endregion

    public static T GetDynamicValue<T>(this Base speckleObject, string member)
    {
      var members = speckleObject.GetMembers();
      if (members.ContainsKey(member))
      {
        if (speckleObject[member] is T)
        {
          return (T)speckleObject[member];
        }

        T retValue = default(T);
        try
        {
          retValue = (T)Convert.ChangeType(speckleObject[member], typeof(T));
        }
        catch { }

        return retValue;
      }
      return default(T);
    }

    public static T GetDynamicEnum<T>(this Base speckleObject, string member) where T : struct
    {
      var members = speckleObject.GetMembers();
      if (members.ContainsKey(member) && speckleObject[member] is string)
      {
        return Enum.TryParse(speckleObject[member] as string, true, out T v) ? v : default(T);
      }
      return default(T);
    }

    public static List<double> Insert(this List<double> source, double item, int step)
    {
      //Inset item into source at every nth index
      //e.g. var source = new List<double>(){ 1, 2, 3, 4, 5, 6 };
      // double item = 0;
      // int step = 3;
      // var output = new List<double>(){ 1, 2, 0, 3, 4, 0, 5, 6, 0 };
      //
      var output = new List<double>();
      for (var i = 0; i < source.Count(); i++)
      {
        output.Add(source[i]);
        if ((i+1) % (step-1) == 0) output.Add(item);
      }
      return output;
    }

    public static bool ValidateCoordinates(List<double> coords, out List<int> nodeIndices)
    {
      nodeIndices = new List<int>();
      for (var i = 0; i < coords.Count(); i += 3)
      {
        var nodeIndex = Instance.GsaModel.Proxy.NodeAt(coords[i], coords[i + 1], coords[i + 2], Instance.GsaModel.CoincidentNodeAllowance);
        if (nodeIndices.Contains(nodeIndex))
        {
          //Two nodes resolve to the same node
          return false;
        }
        nodeIndices.Add(nodeIndex);
      }
      return true;
    }

    public static Colour ColourToNative(this string speckleColour)
    {
      return Enum.TryParse(speckleColour, true, out Colour gsaColour) ? gsaColour : Colour.NO_RGB;
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

    public static void UpsertDictionary<T, U>(this Dictionary<T, List<U>> d, T key, U value)
    {
      if (!d.ContainsKey(key))
      {
        d.Add(key, new List<U>());
      }
      if (!d[key].Contains(value))
      {
        d[key].Add(value);
      }
    }

    public static void UpsertDictionary<T, U>(this Dictionary<T, List<U>> d, T key, IEnumerable<U> values)
    {
      if (!d.ContainsKey(key))
      {
        d.Add(key, values.ToList());
      }
      foreach (var v in values)
      {
        if (!d[key].Contains(v))
        {
          d[key].Add(v);
        }
      }
    }

    public static string RemoveWhitespace(this string input)
    {
      var len = input.Length;
      var src = input.ToCharArray();
      int dstIdx = 0;
      for (int i = 0; i < len; i++)
      {
        var ch = src[i];
        switch (ch)
        {
          case '\u0020':
          case '\u00A0':
          case '\u1680':
          case '\u2000':
          case '\u2001':
          case '\u2002':
          case '\u2003':
          case '\u2004':
          case '\u2005':
          case '\u2006':
          case '\u2007':
          case '\u2008':
          case '\u2009':
          case '\u200A':
          case '\u202F':
          case '\u205F':
          case '\u3000':
          case '\u2028':
          case '\u2029':
          case '\u0009':
          case '\u000A':
          case '\u000B':
          case '\u000C':
          case '\u000D':
          case '\u0085':
            continue;
          default:
            src[dstIdx++] = ch;
            break;
        }
      }
      return new string(src, 0, dstIdx);
    }
    
    //https://stackoverflow.com/questions/23921210/grouping-lists-into-groups-of-x-items-per-group
    public static IEnumerable<IGrouping<int, TSource>> GroupBy<TSource>(this IEnumerable<TSource> source, int itemsPerGroup)
    {
      return source.Zip(Enumerable.Range(0, source.Count()),
                        (s, r) => new { Group = r / itemsPerGroup, Item = s })
                   .GroupBy(i => i.Group, g => g.Item)
                   .ToList();
    }
  }
}
