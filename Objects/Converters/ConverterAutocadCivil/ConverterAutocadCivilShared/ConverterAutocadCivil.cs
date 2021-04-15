using System;
using System.Collections.Generic;
using System.Linq;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Arc = Objects.Geometry.Arc;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
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
#if CIVIL2021
using Civil = Autodesk.Civil;
using CivilDB = Autodesk.Civil.DatabaseServices;
#endif


namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil : ISpeckleConverter
  {
#if AUTOCAD2021
    public static string AutocadAppName = Applications.Autocad2021;
#elif CIVIL2021
    public static string AutocadAppName = Applications.Civil2021;
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
          // set test material
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

        case Polyline o:
          return PolylineToNativeDB(o);

        case Polycurve o:
          return PolycurveToNativeDB(o);

        //case Interval o: // TODO: NOT TESTED
        //  return IntervalToNative(o);

        //case Plane o: // TODO: NOT TESTED
        //  return PlaneToNative(o);

        case Curve o:
          return CurveToNativeDB(o);

        //case Surface o: // TODO: NOT TESTED
        //  return SurfaceToNative(o);

        //case Brep o: // TODO: NOT TESTED
        //  return BrepToNativeDB(o);

        //case Mesh o: // unstable, do not use for now
        //  return MeshToNativeDB(o);

        case BlockInstance o:
          return BlockInstanceToNativeDB(o);

        case BlockDefinition o:
          return BlockDefinitionToNativeDB(o);

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
      switch (obj)
      {
        case DBPoint o:
          return PointToSpeckle(o);

        case AcadDB.Line o:
          return LineToSpeckle(o);

        case AcadDB.Arc o:
          return ArcToSpeckle(o);

        case AcadDB.Circle o:
          return CircleToSpeckle(o);

        case AcadDB.Ellipse o:
          return EllipseToSpeckle(o);

        case AcadDB.Spline o:
          return SplineToSpeckle(o);

        case AcadDB.Polyline o:
          if (o.IsOnlyLines) // db polylines can have arc segments, decide between polycurve or polyline conversion
            return PolylineToSpeckle(o);
          else return PolycurveToSpeckle(o);

        case AcadDB.Polyline3d o:
          return PolylineToSpeckle(o);

        case AcadDB.Polyline2d o:
          return PolycurveToSpeckle(o);

        case PlaneSurface o:
          return SurfaceToSpeckle(o);

         case AcadDB.NurbSurface o:
           return SurfaceToSpeckle(o);

        case AcadDB.PolyFaceMesh o:
          return MeshToSpeckle(o);

        case SubDMesh o:
          return MeshToSpeckle(o);

        case BlockReference o:
          return BlockReferenceToSpeckle(o);

        case BlockTableRecord o:
          return BlockRecordToSpeckle(o);

#if CIVIL2021
        case CivilDB.FeatureLine o:
          return FeatureLineToSpeckle(o);
#endif

        default:
          return null;
      }
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject o:
          switch (o)
          {
            case DBPoint _:
              return true;

            case AcadDB.Line _:
              return true;

            case AcadDB.Arc _:
              return true;

            case AcadDB.Circle _:
              return true;

            case AcadDB.Ellipse _:
              return true;

            case AcadDB.Spline _:
              return true;

            case AcadDB.Polyline _:
              return true;

            case AcadDB.Polyline2d _:
              return true;

            case AcadDB.Polyline3d _:
              return true;

            case AcadDB.PlaneSurface _:
              return true;

            case AcadDB.NurbSurface _:
              return true;

            case AcadDB.PolyFaceMesh _:
              return true;

            case SubDMesh _:
              return true;

            case BlockReference _:
              return true;

            case BlockTableRecord _:
              return true;

            default:
              return false;
          }

        case Acad.Geometry.Point3d _:
          return true;

        case Acad.Geometry.Vector3d _:
          return true;

        case Acad.Geometry.Plane _:
          return true;

        case Acad.Geometry.Line3d _:
          return true;

        case Acad.Geometry.LineSegment3d _:
          return true;

        case Acad.Geometry.CircularArc3d _:
          return true;

        case Acad.Geometry.Curve3d _:
          return true;

        case Acad.Geometry.NurbSurface _:
          return false;

        default:
          return false;

      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
          return true;

        case Line _:
          return true;

        case Arc _:
          return true;

        case Circle _:
          return true;

        case Ellipse _:
          return true;

        case Polyline _:
          return true;

        case Polycurve _:
          return true;

        case Curve _:
          return true;

        case Surface _:
          return false;

        case Brep _:
          return false;

        case Mesh _:
          return false;

        case BlockDefinition _:
          return true;

        case BlockInstance _:
          return true;

        default:
          return false;
      }
    }

  }
}
