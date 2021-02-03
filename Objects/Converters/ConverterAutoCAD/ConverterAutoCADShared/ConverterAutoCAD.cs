using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Arc = Objects.Geometry.Arc;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using AC = Autodesk.AutoCAD;

namespace Objects.Converter.AutoCAD
{
  public partial class ConverterAutoCAD : ISpeckleConverter
  {
#if AUTOCAD2021
    public static string AutoCADAppName = Applications.AutoCAD2021;
#else
    public static string AutoCADAppName = Applications.AutoCAD2021;
#endif

    #region ISpeckleConverter props

    public string Description => "Default Speckle Kit for AutoCAD";
    public string Name => nameof(ConverterAutoCAD);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { AutoCADAppName };

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    #endregion ISpeckleConverter props

    public Document Doc { get; private set; }

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();

    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;
    }

    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject o:
          // add color info and etc here
          return ObjectToSpeckle(o);

        case AC.Geometry.Point3d o:
          return PointToSpeckle(o);

        case AC.Geometry.Vector3d o:
          return VectorToSpeckle(o);

        case AC.Geometry.Line3d o:
          return LineToSpeckle(o);

        case AC.Geometry.LineSegment3d o:
          return LineToSpeckle(o);

        case AC.Geometry.CircularArc3d o:
          return ArcToSpeckle(o);

        case AC.Geometry.Plane o:
          return PlaneToSpeckle(o);

        case AC.Geometry.Curve3d o:
          return CurveToSpeckle(o) as Base;

        case AC.Geometry.NurbSurface o:
          return SurfaceToSpeckle(o);

        default:
          throw new NotSupportedException();
      }
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    // note: currently this returns the DB object, NOT the AC.Geometry object!!! In order to bake into AC.
    // ask about this later, should there be an option to toggle between the two?
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

        case Interval o:
          return IntervalToNative(o);

        case Plane o:
          return PlaneToNative(o);

        case Curve o:
          return CurveToNativeDB(o);

        case Surface o:
          return SurfaceToNative(o);

        default:
          throw new NotSupportedException();
      }
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject _:
          return CanConvertToSpeckle(@object as DBObject);

        case AC.Geometry.Point3d _:
          return true;

        case AC.Geometry.Plane _:
          return true;

        case AC.Geometry.Line3d _:
          return true;

        default:
          return false;
      }
    }

    public bool CanConvertToSpeckle(DBObject @object)
    {
      switch (@object)
      {
        case DBPoint _:
          return true;

        case AC.DatabaseServices.Line _:
          return true;

        case AC.DatabaseServices.Arc _:
          return true;

        case AC.DatabaseServices.Circle _:
          return true;

        case AC.DatabaseServices.Ellipse _:
          return true;

        case AC.DatabaseServices.Polyline _:
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
          return true;

        case Line _:
          return true;

        case Polycurve _:
          return true;

        case Curve _:
          return true;

        default:
          return false;
      }
    }

    public void AddObjectToBlockTableRecord(Entity obj)
    {
      using (DocumentLock l = Doc.LockDocument())
      {
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          BlockTable blkTbl = tr.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
          BlockTableRecord blkTblRec = tr.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
          blkTblRec.AppendEntity(obj);
          tr.AddNewlyCreatedDBObject(obj, true);

          tr.Commit();
        }
      }
    }
  }
}
