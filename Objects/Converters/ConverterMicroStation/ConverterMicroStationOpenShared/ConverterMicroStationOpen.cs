using Objects.Geometry;
using Objects.Primitive;
using Objects.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Ellipse = Objects.Geometry.Ellipse;
using Hatch = Objects.Other.Hatch;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using ModelCurve = Objects.BuiltElements.Revit.Curve.ModelCurve;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using View3D = Objects.BuiltElements.View3D;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using Alignment = Objects.BuiltElements.Alignment;
using Station = Objects.BuiltElements.Station;

using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.Elements;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;
using Bentley.ECObjects.Schema;
using Bentley.ECObjects;
using Bentley.ECObjects.Instance;
using Bentley.EC.Persistence.Query;

#if(OPENBUILDINGS)
using Bentley.Building.Api;
#endif

#if (OPENROADS || OPENRAIL)
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.SDK;
#endif

namespace Objects.Converter.MicroStationOpen
{
  public partial class ConverterMicroStationOpen : ISpeckleConverter
  {
#if MICROSTATION
    public static string BentleyAppName = Applications.MicroStation;
#elif OPENROADS
    public static string BentleyAppName = Applications.OpenRoads;
#elif OPENRAIL
    public static string BentleyAppName = Applications.OpenRail;
#elif OPENBUILDINGS
    public static string BentleyAppName = Applications.OpenBuildings;
#endif
    public string Description => "Default Speckle Kit for MicroStation, OpenRoads, OpenRail and OpenBuildings";
    public string Name => nameof(ConverterMicroStationOpen);
    public string Author => "Arup";
    public string WebsiteOrEmail => "https://www.arup.com";
    public IEnumerable<string> GetServicedApplications() => new string[] { BentleyAppName };
    public ProgressReport Report { get; private set; } = new ProgressReport();
    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();
    public Session Session { get; private set; }
    public DgnFile Doc { get; private set; }
    public DgnModel Model { get; private set; }

#if (OPENROADS || OPENRAIL)
    public GeometricModel GeomModel { get; private set; }
#endif
    public double UoR { get; private set; }
    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();
    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;
    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();

    public void SetContextDocument(object session)
    {
      Session = (Session)session;
      Doc = (DgnFile)Session.GetActiveDgnFile();
      Model = (DgnModel)Session.GetActiveDgnModel();
      UoR = Model.GetModelInfo().UorPerMaster;
#if (OPENROADS || OPENRAIL)
      ConsensusConnection sdkCon = Bentley.CifNET.SDK.Edit.ConsensusConnectionEdit.GetActive();
      GeomModel = sdkCon.GetActiveGeometricModel();
#endif
    }

    public Base ConvertToSpeckle(object @object)
    {
      Base @base = null;

      switch (@object)
      {
        case DPoint2d o:
          @base = Point2dToSpeckle(o);
          break;
        case Point2d o:
          @base = Point2dToSpeckle(o);
          break;
        case DPoint3d o:
          @base = Point3dToSpeckle(o);
          break;
        case Point3d o:
          @base = Point3dToSpeckle(o);
          break;
        case DVector2d o:
          @base = Vector2dToSpeckle(o);
          break;
        case DVector3d o:
          @base = Vector3dToSpeckle(o);
          break;
        case DRange1d o:
          @base = IntervalToSpeckle(o);
          break;
        case DSegment1d o:
          @base = IntervalToSpeckle(o);
          break;
        case DRange2d o:
          @base = Interval2dToSpeckle(o);
          break;
        case DRange3d o:
          @base = BoxToSpeckle(o);
          break;
        case LineElement o:
          @base = LineToSpeckle(o);
          break;
        case DSegment3d o:
          @base = LineToSpeckle(o);
          break;
        case DPlane3d o:
          @base = PlaneToSpeckle(o);
          break;
        case ShapeElement o:
          @base = ShapeToSpeckle(o);
          break;
        case ArcElement o:
          @base = ArcToSpeckle(o) as Base; //Arc, curve or circle
          break;
        case EllipseElement o:
          @base = EllipseToSpeckle(o) as Base; //Ellipse (with or without rotation) or circle
          break;
        case LineStringElement o:
          @base = PolylineToSpeckle(o); //Polyline
          break;
        case ComplexStringElement o:
          @base = PolycurveToSpeckle(o); //Polycurve
          break;
        case BSplineCurveElement o:
          @base = BSplineCurveToSpeckle(o); //Nurbs curve
          break;
        case ComplexShapeElement o:
          @base = ComplexShapeToSpeckle(o);
          break;
        case MeshHeaderElement o:
          @base = MeshToSpeckle(o);
          break;
        case BSplineSurfaceElement o:
          @base = SurfaceToSpeckle(o);
          break;
        case ExtendedElementElement o:
          @base = ExtendedElementToSpeckle(o);
          break;
        case CellHeaderElement o:
          @base = CellHeaderElementToSpeckle(o);
          break;
        case Type2Element o:
          @base = Type2ElementToSpeckle(o);
          break;
#if (OPENBUILDINGS)
        case ITFDrawingGrid o:
          @base = GridSystemsToSpeckle(o);
          break;
#endif
#if (OPENROADS || OPENRAIL)
        case Bentley.CifNET.GeometryModel.SDK.Alignment o:
          @base = AlignmentToSpeckle(o);
          break;
        case Corridor o:
          @base = CorridorToSpeckle(o);
          break;
        case Profile o:
          @base = ProfileToSpeckle(o);
          break;
        case FeaturizedModelEntity o:
          @base = FeatureLineToSpeckle(o);
          break;
#endif
        default:
          throw new NotSupportedException();
      }

      return @base;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public Base ConvertToSpeckleBE(object @object)
    {
      throw new NotImplementedException();
    }

    public List<Base> ConvertToSpeckleBE(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckleBE(x)).ToList();
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point o:
          return PointToNative(o);

        case Vector o:
          return VectorToNative(o);

        case Interval o:
          return IntervalToNative(o);

        case Interval2d o:
          return Interval2dToNative(o);

        case Line o:
          return LineToNative(o);

        case Plane o:
          return PlaneToNative(o);

        case Circle o:
          return CircleToNative(o);

        case Arc o:
          return ArcToNative(o);

        case Ellipse o:
          return EllipseToNative(o);

        case Polyline o:
          return PolylineToNative(o);

        case Polycurve o:
          return PolycurveToNative(o); // polycurve converted to complex chain

        case Curve o:
          return CurveToNative(o);

        case Box o:
          return BoxToNative(o);

        case Mesh o:
          return MeshToNative(o);

#if (OPENROADS || OPENRAIL)
        case Alignment o:
          return AlignmentToNative(o);
#endif
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
        case DPoint2d _:
        case Point2d _:
        case DPoint3d _:
        case Point3d _:
        case DVector2d _:
        case DVector3d _:
        case LineElement _:
        case Bentley.GeometryNET.LineString _:
        case LineStringElement _:
        case ArcElement _:
        case ComplexStringElement _:
        case EllipseElement _:
        case BSplineCurveElement _:
        case ShapeElement _:
        case ComplexShapeElement _:
        case MeshHeaderElement _:
        //case BSplineSurfaceElement _:
        case ExtendedElementElement _:
        case CellHeaderElement _:
        case Type2Element _: //Complex header element
          return true;

#if (OPENROADS || OPENRAIL)
        case Bentley.CifNET.GeometryModel.SDK.Alignment _:
          //case Corridor _:
          //case Profile _:
          //case FeaturizedModelEntity _:
          return true;
#endif

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
        case Vector _:
        case Interval _:
        case Interval2d _:
        case Line _:
        case Plane _:
        case Circle _:
        case Arc _:
        case Ellipse _:
        case Polyline _:
        case Polycurve _:
        case Curve _:
        case Box _:
        case Mesh _:
          //case Surface _:
          //case Alignment _:                    ;
          return true;

        //TODO: This types are not supported in Bentley connectors!
        case Brep _:
        case Pointcloud _:
        case ModelCurve _:
        case DirectShape _:
        case View3D _:
        case BlockDefinition _:
        case BlockInstance _:
        case Hatch _:
          return true;

        default:
          return false;
      }
    }
  }
}