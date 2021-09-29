using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Objects.Other;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Hatch = Objects.Other.Hatch;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using ModelCurve = Objects.BuiltElements.Revit.Curve.ModelCurve;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Acad = Autodesk.AutoCAD;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
#if (CIVIL2021 || CIVIL2022)
using Civil = Autodesk.Civil;
using CivilDB = Autodesk.Civil.DatabaseServices;
#endif


namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil : ISpeckleConverter
  {
#if AUTOCAD2021
    public static string AutocadAppName = Applications.Autocad2021;
#elif AUTOCAD2022
public static string AutocadAppName = Applications.Autocad2022;
#elif CIVIL2021
    public static string AutocadAppName = Applications.Civil2021;
#elif CIVIL2022
    public static string AutocadAppName = Applications.Civil2022;
#endif

    #region ISpeckleConverter props

    public string Description => "Default Speckle Kit for AutoCAD";
    public string Name => nameof(ConverterAutocadCivil);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { AutocadAppName };

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    #endregion ISpeckleConverter props

    public Document Doc { get; private set; }
    public Transaction Trans { get; private set; } // TODO: evaluate if this should be here

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();

    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;
      Trans = Doc.TransactionManager.TopTransaction; // set the stream transaction here! make sure it is the top level transaction
    }

    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject o:
          /*
          // check for speckle schema xdata
          string schema = GetSpeckleSchema(o.XData);
          if (schema != null)
            return ObjectToSpeckleBuiltElement(o);
          */
          return ObjectToSpeckle(o);

        case Acad.Geometry.Point3d o:
          return PointToSpeckle(o);

        case Acad.Geometry.Vector3d o:
          return VectorToSpeckle(o);

        case Acad.Geometry.Line3d o:
          return LineToSpeckle(o);

        case Acad.Geometry.LineSegment3d o:
          return LineToSpeckle(o);

        case Acad.Geometry.CircularArc3d o:
          return ArcToSpeckle(o);

        case Acad.Geometry.Plane o:
          return PlaneToSpeckle(o);

        case Acad.Geometry.Curve3d o:
          return CurveToSpeckle(o) as Base;

        default:
          throw new NotSupportedException();
      }
    }

    private Base ObjectToSpeckleBuiltElement(DBObject o)
    {
      throw new NotImplementedException();
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point o:
          return PointToNativeDB(o);

        case Line o:
          return LineToNativeDB(o);

        case Arc o:
          return ArcToNativeDB(o);

        case Circle o:
          return CircleToNativeDB(o);

        case Ellipse o:
          return EllipseToNativeDB(o);

        case Hatch o:
          return HatchToNativeDB(o);

        case Polyline o:
          return PolylineToNativeDB(o);

        case Polycurve o:
          var splineSegments = o.segments.Where(s => s is Curve);
          if (splineSegments.Count() > 0)
            return PolycurveSplineToNativeDB(o);
          else
            return PolycurveToNativeDB(o);

        //case Interval o: // TODO: NOT TESTED
        //  return IntervalToNative(o);

        //case Plane o: // TODO: NOT TESTED
        //  return PlaneToNative(o);

        case Curve o:
          return CurveToNativeDB(o);

        /*
        case Surface o: 
          return SurfaceToNative(o);
        */

        case Brep o:
          if (o.displayMesh != null)
            return MeshToNativeDB(o.displayMesh);
          else
            return null;

        case Mesh o:
          return MeshToNativeDB(o);

        case BlockInstance o:
          return BlockInstanceToNativeDB(o, out BlockReference refernce);

        case BlockDefinition o:
          return BlockDefinitionToNativeDB(o);

        case Text o:
          bool isMText = o["isMText"] as bool? ?? true;
          if (!isMText)
            return DBTextToNative(o);
          return MTextToNative(o);

        // TODO: add Civil3D directive to convert to alignment instead of curve
        case Alignment o:
          return CurveToNativeDB(o.baseCurve);

        case ModelCurve o:
          return CurveToNativeDB(o.baseCurve);

        default:
          throw new NotSupportedException();
      }
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    /// <summary>
    /// Converts a DB Object <see cref="DBObject"/> instance to a Speckle <see cref="Base"/>
    /// </summary>
    /// <param name="obj">DB Object to be converted.</param>
    /// <returns></returns>
    public Base ObjectToSpeckle(DBObject obj)
    {
      Base @base = null;
      DisplayStyle style = GetStyle(obj);

      switch (obj)
      {
        case DBPoint o:
          @base = PointToSpeckle(o);
          break;
        case AcadDB.Line o:
          @base = LineToSpeckle(o);
          break;
        case AcadDB.Arc o:
          @base = ArcToSpeckle(o);
          break;
        case AcadDB.Circle o:
          @base = CircleToSpeckle(o);
          break;
        case AcadDB.Ellipse o:
          @base = EllipseToSpeckle(o);
          break;
        case AcadDB.Hatch o:
          @base = HatchToSpeckle(o);
          break;
        case AcadDB.Spline o:
          @base = SplineToSpeckle(o);
          break;
        case AcadDB.Polyline o:
          if (o.IsOnlyLines) // db polylines can have arc segments, decide between polycurve or polyline conversion
            @base = PolylineToSpeckle(o);
          else 
            @base = PolycurveToSpeckle(o);
          break;
        case AcadDB.Polyline3d o:
          @base = PolylineToSpeckle(o);
          break;
        case AcadDB.Polyline2d o:
          @base = PolycurveToSpeckle(o);
          break;
        case Region o:
          @base = RegionToSpeckle(o);
          break;
        case AcadDB.Surface o:
          @base = SurfaceToSpeckle(o);
          break;
        case AcadDB.PolyFaceMesh o:
          @base = MeshToSpeckle(o);
          break;
        case SubDMesh o:
          @base = MeshToSpeckle(o);
          break;
        case Solid3d o:
          @base = SolidToSpeckle(o);
          break;
        case BlockReference o:
          @base = BlockReferenceToSpeckle(o);
          break;
        case BlockTableRecord o:
          @base = BlockRecordToSpeckle(o);
          break;
        case AcadDB.DBText o:
          @base = TextToSpeckle(o);
          break;
        case AcadDB.MText o:
          @base = TextToSpeckle(o);
          break;
#if (CIVIL2021 || CIVIL2022)
        case CivilDB.Alignment o:
          @base = AlignmentToSpeckle(o);
          break;
        case CivilDB.Corridor o:
          @base = CorridorToSpeckle(o);
          break;
        case CivilDB.FeatureLine o:
          @base = FeatureLineToSpeckle(o);
          break;
        case CivilDB.Structure o:
          @base = StructureToSpeckle(o);
          break;
        case CivilDB.Pipe o:
          @base = PipeToSpeckle(o);
          break;
        case CivilDB.Profile o:
          @base = ProfileToSpeckle(o);
          break;
        case CivilDB.TinSurface o:
          @base = SurfaceToSpeckle(o);
          break;
#endif
      }
      if (style != null)
        @base["displayStyle"] = style;

      return @base;
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject o:
          switch (o)
          {
            case DBPoint _:
            case AcadDB.Line _:
            case AcadDB.Arc _:
            case AcadDB.Circle _:
            case AcadDB.Ellipse _:
            case AcadDB.Hatch _:
            case AcadDB.Spline _:
            case AcadDB.Polyline _:
            case AcadDB.Polyline2d _:
            case AcadDB.Polyline3d _:
            case AcadDB.Surface _:
            case AcadDB.PolyFaceMesh _:
            case AcadDB.Region _:
            case SubDMesh _:
            case Solid3d _:
              return true;

            case BlockReference _:
            case BlockTableRecord _:
            case AcadDB.DBText _:
            case AcadDB.MText _:
              return true;

#if (CIVIL2021 || CIVIL2022)
// NOTE: C3D pressure pipes and pressure fittings API under development
            case CivilDB.FeatureLine _:
            case CivilDB.Corridor _:
            case CivilDB.Structure _:
            case CivilDB.Alignment _:
            case CivilDB.Pipe _:
            case CivilDB.Profile _:
            case CivilDB.TinSurface _:
              return true;
#endif

            default:
              return false;
          }

        case Acad.Geometry.Point3d _:
        case Acad.Geometry.Vector3d _:
        case Acad.Geometry.Plane _:
        case Acad.Geometry.Line3d _:
        case Acad.Geometry.LineSegment3d _:
        case Acad.Geometry.CircularArc3d _:
        case Acad.Geometry.Curve3d _:
          return true;

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
        case Line _:
        case Arc _:
        case Circle _:  
        case Ellipse _:
        case Hatch _:
        case Polyline _:
        case Polycurve _:
        case Curve _:
        case Brep _:
        case Mesh _:

        case BlockDefinition _:
        case BlockInstance _:
        case Text _:

        case Alignment _:

        case ModelCurve _:
          return true;

        default:
          return false;
      }
    }

  }
}
