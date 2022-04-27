using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Acad = Autodesk.AutoCAD;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Hatch = Objects.Other.Hatch;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using ModelCurve = Objects.BuiltElements.Revit.Curve.ModelCurve;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Spiral = Objects.Geometry.Spiral;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
#if (CIVIL2021 || CIVIL2022)
using Civil = Autodesk.Civil;
using CivilDB = Autodesk.Civil.DatabaseServices;
#endif


namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil : ISpeckleConverter
  {
#if AUTOCAD2021
    public static string AutocadAppName = VersionedHostApplications.Autocad2021;
#elif AUTOCAD2022
public static string AutocadAppName = VersionedHostApplications.Autocad2022;
#elif CIVIL2021
    public static string AutocadAppName = VersionedHostApplications.Civil2021;
#elif CIVIL2022
    public static string AutocadAppName = VersionedHostApplications.Civil2022;
#endif

    public ConverterAutocadCivil()
    {
      var ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterAutocadCivil)).GetName().Version;
      Report.Log($"Using converter: {Name} v{ver}");
    }

    #region ISpeckleConverter props
    public string Description => "Default Speckle Kit for AutoCAD";
    public string Name => nameof(ConverterAutocadCivil);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";
    public ProgressReport Report { get; private set; } = new ProgressReport();
    public IEnumerable<string> GetServicedApplications() => new string[] { AutocadAppName };
    public Document Doc { get; private set; }
    public Transaction Trans { get; private set; } // TODO: evaluate if this should be here
    #endregion ISpeckleConverter props

    public ReceiveMode ReceiveMode { get; set; }

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();
    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }

    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;
      Trans = Doc.TransactionManager.TopTransaction; // set the stream transaction here! make sure it is the top level transaction
      Report.Log($"Using document: {Doc.Name}");
      Report.Log($"Using units: {ModelUnits}");
    }

    public Base ConvertToSpeckle(object @object)
    {
      Base @base = null;
      switch (@object)
      {
        case DBObject obj:
          /*
          // check for speckle schema xdata
          string schema = GetSpeckleSchema(o.XData);
          if (schema != null)
            return ObjectToSpeckleBuiltElement(o);
          */
          switch (obj)
          {
            case DBPoint o:
              @base = PointToSpeckle(o);
              Report.Log($"Converted Point3d {o}");
              break;
            case AcadDB.Line o:
              @base = LineToSpeckle(o);
              Report.Log($"Converted Line");
              break;
            case AcadDB.Arc o:
              @base = ArcToSpeckle(o);
              Report.Log($"Converted Arc");
              break;
            case AcadDB.Circle o:
              @base = CircleToSpeckle(o);
              Report.Log($"Converted Circle");
              break;
            case AcadDB.Ellipse o:
              @base = EllipseToSpeckle(o);
              Report.Log($"Converted Ellipse");
              break;
            case AcadDB.Hatch o:
              @base = HatchToSpeckle(o);
              Report.Log($"Converted Hatch");
              break;
            case AcadDB.Spline o:
              @base = SplineToSpeckle(o);
              Report.Log($"Converted Spline");
              break;
            case AcadDB.Polyline o:
              if (o.IsOnlyLines) // db polylines can have arc segments, decide between polycurve or polyline conversion
              {
                @base = PolylineToSpeckle(o);
                Report.Log($"Converted Polyline as Polyline");
              }
              else
              {
                @base = PolycurveToSpeckle(o);
                Report.Log($"Converted Polyline as Polycurve");
              }
              break;
            case AcadDB.Polyline3d o:
              @base = PolylineToSpeckle(o);
              Report.Log($"Converted Polyline3d");
              break;
            case AcadDB.Polyline2d o:
              @base = PolycurveToSpeckle(o);
              Report.Log($"Converted Polyline2d as Polycurve");
              break;
            case Region o:
              @base = RegionToSpeckle(o);
              Report.Log($"Converted Region as Mesh");
              break;
            case AcadDB.Surface o:
              @base = SurfaceToSpeckle(o);
              Report.Log($"Converted Surface as Mesh");
              break;
            case AcadDB.PolyFaceMesh o:
              @base = MeshToSpeckle(o);
              Report.Log($"Converted PolyFace Mesh");
              break;
            case SubDMesh o:
              @base = MeshToSpeckle(o);
              Report.Log($"Converted SubD Mesh");
              break;
            case Solid3d o:
              if (o.IsNull)
              {
                Report.Log($"Skipped null Solid");
                return null;
              }
              @base = SolidToSpeckle(o);
              Report.Log($"Converted Solid as Mesh");
              break;
            case BlockReference o:
              @base = BlockReferenceToSpeckle(o);
              Report.Log($"Converted Block Instance");
              break;
            case BlockTableRecord o:
              @base = BlockRecordToSpeckle(o);
              Report.Log($"Converted Block Definition");
              break;
            case AcadDB.DBText o:
              @base = TextToSpeckle(o);
              Report.Log($"Converted Text");
              break;
            case AcadDB.MText o:
              @base = TextToSpeckle(o);
              Report.Log($"Converted Text");
              break;
#if (CIVIL2021 || CIVIL2022)
            case CivilDB.Alignment o:
              @base = AlignmentToSpeckle(o);
              Report.Log($"Converted Alignment");
              break;
            case CivilDB.Corridor o:
              @base = CorridorToSpeckle(o);
              Report.Log($"Converted Corridor as Base");
              break;
            case CivilDB.FeatureLine o:
              @base = FeatureLineToSpeckle(o);
              Report.Log($"Converted FeatureLine");
              break;
            case CivilDB.Structure o:
              @base = StructureToSpeckle(o);
              Report.Log($"Converted Structure");
              break;
            case CivilDB.Pipe o:
              @base = PipeToSpeckle(o);
              Report.Log($"Converted Pipe");
              break;
            case CivilDB.PressurePipe o:
              @base = PipeToSpeckle(o);
              Report.Log($"Converted Pressure Pipe");
              break;
            case CivilDB.Profile o:
              @base = ProfileToSpeckle(o);
              Report.Log($"Converted Profile as Base");
              break;
            case CivilDB.TinSurface o:
              @base = SurfaceToSpeckle(o);
              Report.Log($"Converted TIN Surface as mesh");
              break;
#endif
          }

          DisplayStyle style = DisplayStyleToSpeckle(obj as Entity);
          if (style != null)
            @base["displayStyle"] = style;
          break;

        case Acad.Geometry.Point3d o:
          @base = PointToSpeckle(o);
          Report.Log($"Converted Point3d {o}");
          break;

        case Acad.Geometry.Vector3d o:
          @base = VectorToSpeckle(o);
          Report.Log($"Converted Vector3d {o}");
          break;

        case Acad.Geometry.Line3d o:
          @base = LineToSpeckle(o);
          Report.Log($"Converted Line3d");
          break;

        case Acad.Geometry.LineSegment3d o:
          @base = LineToSpeckle(o);
          Report.Log($"Converted LineSegment");
          break;

        case Acad.Geometry.CircularArc3d o:
          @base = ArcToSpeckle(o);
          Report.Log($"Converted Arc3d");
          break;

        case Acad.Geometry.Plane o:
          @base = PlaneToSpeckle(o);
          Report.Log($"Converted Plane");
          break;

        case Acad.Geometry.Curve3d o:
          @base = CurveToSpeckle(o) as Base;
          Report.Log($"Converted Curve3d");
          break;

        default:
          Report.Log($"Skipped not supported type: {@object.GetType()}");
          throw new NotSupportedException();
      }
      return @base;
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
      object acadObj = null;
      switch (@object)
      {
        case Point o:
          acadObj = PointToNativeDB(o);
          Report.Log($"Created Point {o.id}");
          break;

        case Line o:
          acadObj = LineToNativeDB(o);
          Report.Log($"Created Line {o.id}");
          break;

        case Arc o:
          acadObj = ArcToNativeDB(o);
          Report.Log($"Created Arc {o.id}");
          break;

        case Circle o:
          acadObj = CircleToNativeDB(o);
          Report.Log($"Created Circle {o.id}");
          break;

        case Ellipse o:
          acadObj = EllipseToNativeDB(o);
          Report.Log($"Created Ellipse {o.id}");
          break;

        case Spiral o:
          acadObj = PolylineToNativeDB(o.displayValue);
          Report.Log($"Created Spiral {o.id} as Polyline");
          break;

        case Hatch o:
          acadObj = HatchToNativeDB(o);
          Report.Log($"Created Hatch {o.id}");
          break;

        case Polyline o:
          acadObj = PolylineToNativeDB(o);
          Report.Log($"Created Polyline {o.id}");
          break;

        case Polycurve o:
          bool convertAsSpline = (o.segments.Where(s => !(s is Line) && !(s is Arc)).Count() > 0) ? true : false;
          if (!convertAsSpline) convertAsSpline = IsPolycurvePlanar(o) ? false : true;
          if (convertAsSpline)
          {
            acadObj = PolycurveSplineToNativeDB(o);
            if (acadObj == null)
              Report.Log($"Created Polycurve {o.id} as individual segments");
            else
              Report.Log($"Created Polycurve {o.id} as Spline");
            break;
          }
          else
          {
            acadObj = PolycurveToNativeDB(o);
            Report.Log($"Created Polycurve {o.id} as Polyline");
            break;
          }

        case Curve o:
          acadObj = CurveToNativeDB(o);
          Report.Log($"Created Curve {o.id}");
          break;

        /*
        case Surface o: 
          return SurfaceToNative(o);

        case Brep o:
          acadObj = (o.displayMesh != null) ? MeshToNativeDB(o.displayMesh) : null;
          Report.Log($"Created Brep {o.id} as Mesh");
          break;
        */

        case Mesh o:
          acadObj = MeshToNativeDB(o);
          Report.Log($"Created Mesh {o.id}");
          break;

        case BlockInstance o:
          acadObj = BlockInstanceToNativeDB(o, out BlockReference reference);
          Report.Log($"Created Block Instance {o.id}");
          break;

        case BlockDefinition o:
          acadObj = BlockDefinitionToNativeDB(o);
          Report.Log($"Created Block Definition {o.id}");
          break;

        case Text o:
          bool isMText = o["isMText"] as bool? ?? true;
          if (!isMText)
            acadObj = DBTextToNative(o);
          acadObj = MTextToNative(o);
          Report.Log($"Created Text {o.id}");
          break;

        case Alignment o:
          string fallback = " as Polyline";
          if (o.curves is null) // TODO: remove after a few releases, this is for backwards compatibility
          {
            acadObj = CurveToNativeDB(o.baseCurve);
            Report.Log($"Created Alignment {o.id} as Curve");
            break;
          }
#if (CIVIL2020 || CIVIL2021)
          acadObj = AlignmentToNative(o);
          if (acadObj != null)
            fallback = string.Empty;
#endif
          if (acadObj == null)
            acadObj = PolylineToNativeDB(o.displayValue);
          Report.Log($"Created Alignment {o.id}{fallback}");
          break;

        case ModelCurve o:
          acadObj = CurveToNativeDB(o.baseCurve);
          Report.Log($"Created ModelCurve {o.id} as Curve");
          break;

        default:
          Report.Log($"Skipped not supported type: {@object.GetType()} {@object.id}");
          throw new NotSupportedException();
      }

      return acadObj;
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
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
            case CivilDB.PressurePipe _:
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
        case Spiral _:
        case Hatch _:
        case Polyline _:
        case Polycurve _:
        case Curve _:
        //case Brep _:
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
