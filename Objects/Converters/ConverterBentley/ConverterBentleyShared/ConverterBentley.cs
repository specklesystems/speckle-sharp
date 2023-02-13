using Bentley.DgnPlatformNET;
using Bentley.DgnPlatformNET.DgnEC;
using Bentley.DgnPlatformNET.Elements;
using Bentley.EC.Persistence.Query;
using Bentley.ECObjects;
using Bentley.ECObjects.Instance;
using Bentley.ECObjects.Schema;
using Bentley.GeometryNET;
using Bentley.MstnPlatformNET;
using Objects.Geometry;
using Objects.Primitive;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Alignment = Objects.BuiltElements.Alignment;
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
using Station = Objects.BuiltElements.Station;
using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using View3D = Objects.BuiltElements.View3D;

#if(OPENBUILDINGS)
using Bentley.Building.Api;
#endif

#if (OPENROADS || OPENRAIL)
using Bentley.CifNET.GeometryModel.SDK;
using Bentley.CifNET.LinearGeometry;
using Bentley.CifNET.SDK;
#endif

namespace Objects.Converter.Bentley
{
  public partial class ConverterBentley : ISpeckleConverter
  {
#if MICROSTATION
    public static string BentleyAppName = HostApplications.MicroStation.Name;
#elif OPENROADS
    public static string BentleyAppName = HostApplications.OpenRoads.Name;
#elif OPENRAIL
    public static string BentleyAppName = HostApplications.OpenRail.Name;
#elif OPENBUILDINGS
    public static string BentleyAppName = HostApplications.OpenBuildings.Name;
#endif
    public string Description => "Default Speckle Kit for MicroStation, OpenRoads, OpenRail and OpenBuildings";
    public string Name => nameof(ConverterBentley);
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
    public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();
    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;
    public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();
    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }

    public ReceiveMode ReceiveMode { get; set; }

    public void SetContextDocument(object session)
    {
      Session = (Session)session;
      Doc = (DgnFile)Session.GetActiveDgnFile();
      Model = (DgnModel)Session.GetActiveDgnModel();
      UoR = Model.GetModelInfo().UorPerMaster;
#if (OPENROADS || OPENRAIL)
      ConsensusConnection sdkCon = global::Bentley.CifNET.SDK.Edit.ConsensusConnectionEdit.GetActive();
      GeomModel = sdkCon.GetActiveGeometricModel();
#endif
      Report.Log($"Using document: {Doc.GetFileName()}");
      Report.Log($"Using units: {ModelUnits}");
    }

    public Base ConvertToSpeckle(object @object)
    {
      Base @base = null;

      switch (@object)
      {
        case DPoint2d o:
          @base = Point2dToSpeckle(o);
          Report.Log($"Converted DPoint2d {o}");
          break;
        case Point2d o:
          @base = Point2dToSpeckle(o);
          Report.Log($"Converted Point2d {o}");
          break;
        case DPoint3d o:
          @base = Point3dToSpeckle(o);
          Report.Log($"Converted DPoint3d {o}");
          break;
        case Point3d o:
          @base = Point3dToSpeckle(o);
          Report.Log($"Converted Point3d {o}");
          break;
        case DVector2d o:
          @base = Vector2dToSpeckle(o);
          Report.Log($"Converted DVector2d {o}");
          break;
        case DVector3d o:
          @base = Vector3dToSpeckle(o);
          Report.Log($"Converted DVector3d {o}");
          break;
        case DRange1d o:
          @base = IntervalToSpeckle(o);
          Report.Log($"Converted DRange1d {o} as Interval");
          break;
        case DSegment1d o:
          @base = IntervalToSpeckle(o);
          Report.Log($"Converted DSegment1d {o} as Interval");
          break;
        case DRange2d o:
          @base = Interval2dToSpeckle(o);
          Report.Log($"Converted DRange2d {o} as Interval");
          break;
        case DRange3d o:
          @base = BoxToSpeckle(o);
          Report.Log($"Converted DRange3d {o} as Box");
          break;
        case LineElement o:
          @base = LineToSpeckle(o);
          Report.Log($"Converted Line");
          break;
        case DSegment3d o:
          @base = LineToSpeckle(o);
          Report.Log($"Converted DSegment3d as Line");
          break;
        case DPlane3d o:
          @base = PlaneToSpeckle(o);
          Report.Log($"Converted Plane");
          break;
        case ShapeElement o:
          @base = ShapeToSpeckle(o);
          Report.Log($"Converted Shape as Polyline");
          break;
        case ArcElement o:
          @base = ArcToSpeckle(o) as Base; //Arc, curve or circle
          Report.Log($"Converted Arc as ICurve");
          break;
        case EllipseElement o:
          @base = EllipseToSpeckle(o) as Base; //Ellipse (with or without rotation) or circle
          Report.Log($"Converted Ellipse as ICurve");
          break;
        case LineStringElement o:
          @base = PolylineToSpeckle(o); //Polyline
          Report.Log($"Converted LineString as Polyline");
          break;
        case ComplexStringElement o:
          @base = PolycurveToSpeckle(o); //Polycurve
          Report.Log($"Converted ComplexString as Polycurve");
          break;
        case BSplineCurveElement o:
          @base = BSplineCurveToSpeckle(o); //Nurbs curve
          Report.Log($"Converted BSpline as Curve");
          break;
        case ComplexShapeElement o:
          @base = ComplexShapeToSpeckle(o);
          Report.Log($"Converted ComplexShape as Polycurve");
          break;
        case MeshHeaderElement o:
          @base = MeshToSpeckle(o);
          Report.Log($"Converted Mesh");
          break;
        case BSplineSurfaceElement o:
          @base = SurfaceToSpeckle(o);
          Report.Log($"Converted Surface");
          break;
        case ExtendedElementElement o:
          @base = ExtendedElementToSpeckle(o);
          Report.Log($"Converted ExtendedElement as Base");
          break;
        case CellHeaderElement o:
          @base = CellHeaderElementToSpeckle(o);
          Report.Log($"Converted CellHeader as Base");
          break;
        case Type2Element o:
          @base = Type2ElementToSpeckle(o);
          Report.Log($"Converted Type2 as Base");
          break;
#if (OPENBUILDINGS)
        case ITFDrawingGrid o:
          @base = GridSystemsToSpeckle(o);
          Report.Log($"Converted GridSystems as Base");
          break;
#endif
#if (OPENROADS || OPENRAIL)
        case global::Bentley.CifNET.GeometryModel.SDK.Alignment o:
          @base = AlignmentToSpeckle(o);
          Report.Log($"Converted Alignment");
          break;
        case Corridor o:
          @base = CorridorToSpeckle(o);
          Report.Log($"Converted Corridor as Base");
          break;
        case Profile o:
          @base = ProfileToSpeckle(o);
          Report.Log($"Converted Profile as Base");
          break;
        case FeaturizedModelEntity o:
          @base = FeatureLineToSpeckle(o);
          Report.Log($"Converted FeaturizedModel as Base");
          break;
#endif
        default:
          Report.Log($"Skipped not supported type: {@object.GetType()}");
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
          Report.Log($"Created Point {o.id}");
          return PointToNative(o);

        case Vector o:
          Report.Log($"Created Vector {o.id}");
          return VectorToNative(o);

        case Interval o:
          Report.Log($"Created Interval {o.id}");
          return IntervalToNative(o);

        case Interval2d o:
          Report.Log($"Created Interval2d {o.id}");
          return Interval2dToNative(o);

        case Line o:
          Report.Log($"Created Line {o.id}");
          return LineToNative(o);

        case Plane o:
          Report.Log($"Created Plane {o.id}");
          return PlaneToNative(o);

        case Circle o:
          Report.Log($"Created Circle {o.id}");
          return CircleToNative(o);

        case Arc o:
          Report.Log($"Created Arc {o.id}");
          return ArcToNative(o);

        case Ellipse o:
          Report.Log($"Created Ellipse {o.id}");
          return EllipseToNative(o);

        case Polyline o:
          Report.Log($"Created Polyline {o.id}");
          return PolylineToNative(o);

        case Polycurve o:
          Report.Log($"Created Polycurve {o.id} asn ComplexString");
          return PolycurveToNative(o); // polycurve converted to complex chain

        case Curve o:
          Report.Log($"Created Curve {o.id} as DisplayableElement");
          return CurveToNative(o);

        case Box o:
          Report.Log($"Created Box {o.id} as DRange3d");
          return BoxToNative(o);

        case Mesh o:
          Report.Log($"Created Mesh {o.id}");
          return MeshToNative(o);

#if (OPENROADS || OPENRAIL)
        case Alignment o:
          Report.Log($"Created Alignment {o.id}");
          return AlignmentToNative(o);
#endif
        default:
          Report.Log($"Skipped not supported type: {@object.GetType()} {@object.id}");
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
        case global::Bentley.GeometryNET.LineString _:
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
        case global::Bentley.CifNET.GeometryModel.SDK.Alignment _:
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

        default:
          return false;
      }
    }
  }
}