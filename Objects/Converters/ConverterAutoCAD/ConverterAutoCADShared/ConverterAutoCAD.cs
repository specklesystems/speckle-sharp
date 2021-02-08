using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using AC = Autodesk.AutoCAD;

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

    private Base ObjectToSpeckleBuiltElement(DBObject o)
    {
      throw new NotImplementedException();
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

        case Interval o: // TODO: NOT TESTED
          return IntervalToNative(o);

        case Plane o: // TODO: NOT TESTED
          return PlaneToNative(o);

        case Curve o: // TODO: SPLINES AND NURBS NOT TESTED
          return CurveToNativeDB(o);

        case Surface o: // TODO: NOT TESTED
          return SurfaceToNative(o);

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
    /// <remarks>
    /// faster way but less readable method is to check object class name string: obj.ObjectId.ObjectClass.DxfName
    /// https://spiderinnet1.typepad.com/blog/2012/04/various-ways-to-check-object-types-in-autocad-net.html
    /// </remarks>
    public Base ObjectToSpeckle(DBObject obj)
    {
      switch (obj)
      {
        case DBPoint o:
          return PointToSpeckle(o);

        case AC.DatabaseServices.Line o:
          return LineToSpeckle(o);

        case AC.DatabaseServices.Arc o:
          return ArcToSpeckle(o);

        case AC.DatabaseServices.Circle o:
          return CircleToSpeckle(o);

        case AC.DatabaseServices.Ellipse o:
          return EllipseToSpeckle(o);

        case AC.DatabaseServices.Spline o:
          return SplineToSpeckle(o);

        case AC.DatabaseServices.Polyline o:
          if (o.IsOnlyLines) // db polylines can have arc segments, decide between polycurve or polyline conversion
            return PolylineToSpeckle(o);
          else return PolycurveToSpeckle(o);

        case AC.DatabaseServices.Polyline2d o:
          return PolycurveToSpeckle(o);

        default:
          return null;
      }
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DBObject o:
          return CanConvertToSpeckle(o);

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

        case Polyline _:
          return true;

        case Arc _:
          return true;

        case Circle _:
          return true;

        case Ellipse _:
          return true;

        case Polycurve _:
          return true;

        case Curve _:
          return true;

        default:
          return false;
      }
    }
  }
}
