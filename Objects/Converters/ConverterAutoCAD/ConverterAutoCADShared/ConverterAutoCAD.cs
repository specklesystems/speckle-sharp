using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
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
using Polyline = Objects.Geometry.Polyline;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using AC = Autodesk.AutoCAD.Geometry;

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
        case Point3d o:
          return PointToSpeckle(o);

        case Vector3d o:
          return VectorToSpeckle(o);

        case Line3d o:
          return LineToSpeckle(o);

        case AC.Plane o:
          return PlaneToSpeckle(o);

        case PolylineCurve3d o:
          return PolylineToSpeckle(o) as Base;

        case Curve3d o:
          return CurveToSpeckle(o) as Base;

        case NurbSurface o:
          return SurfaceToSpeckle(o);

        default:
          throw new NotSupportedException();
      }
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
          return PointToNative(o);

        case Interval o:
          return IntervalToNative(o);

        case Plane o:
          return PlaneToNative(o);

        case Curve o:
          return CurveToNative(o);

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
        case Point3d _:
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

        default:
          return false;
      }
    }
  }
}
