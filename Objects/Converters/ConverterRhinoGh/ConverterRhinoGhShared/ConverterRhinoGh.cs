using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Objects.Other;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
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
using View3D = Objects.BuiltElements.View3D;

using RH = Rhino.Geometry;

using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh : ISpeckleConverter
  {
#if RHINO6
    public static string RhinoAppName = Applications.Rhino6;
    public static string GrasshopperAppName = Applications.Grasshopper;
#elif RHINO7
    public static string RhinoAppName = Applications.Rhino7;
    public static string GrasshopperAppName = Applications.Grasshopper;
#endif

    public string Description => "Default Speckle Kit for Rhino & Grasshopper";
    public string Name => nameof(ConverterRhinoGh);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications()
    {
      
#if RHINO6
      return new string[] {RhinoAppName, Applications.Grasshopper};
#elif RHINO7
      return new string[] {RhinoAppName};
#endif   
    }

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    public RhinoDoc Doc { get; private set; }

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();

    public void SetContextDocument(object doc)
    {
      Doc = (RhinoDoc)doc;
    }

    // speckle user string for custom schemas
    string SpeckleSchemaKey = "SpeckleSchema";

    public Base ConvertToSpeckle(object @object)
    {
      RenderMaterial material = null;
      DisplayStyle style = null;
      Base @base = null;
      Base schema = null;
      if (@object is RhinoObject ro)
      {
        material = GetMaterial(ro);
        style = GetStyle(ro);

        if (ro.Attributes.GetUserString(SpeckleSchemaKey) != null) // schema check - this will change in the near future
        {
            try
            {
                schema = ConvertToSpeckleBE(ro);
            }
            catch 
            {
            }
        }


        if (!(@object is InstanceObject)) // block instance check
          @object = ro.Geometry;
      }

      switch (@object)
      {
        case Point3d o:
          @base = PointToSpeckle(o);
          break;
        case Rhino.Geometry.Point o:
          @base = PointToSpeckle(o);
          break;
        case PointCloud o:
          @base = PointcloudToSpeckle(o);
          break;
        case Vector3d o:
          @base = VectorToSpeckle(o);
          break;
        case RH.Interval o:
          @base = IntervalToSpeckle(o);
          break;
        case UVInterval o:
          @base = Interval2dToSpeckle(o);
          break;
        case RH.Line o:
          @base = LineToSpeckle(o);
          break;
        case LineCurve o:
          @base = LineToSpeckle(o);
          break;
        case RH.Plane o:
          @base = PlaneToSpeckle(o);
          break;
        case Rectangle3d o:
          @base = PolylineToSpeckle(o);
          break;
        case RH.Circle o:
          @base = CircleToSpeckle(o);
          break;
        case RH.Arc o:
          @base = ArcToSpeckle(o);
          break;
        case ArcCurve o:
          @base = ArcToSpeckle(o);
          break;
        case RH.Ellipse o:
          @base = EllipseToSpeckle(o);
          break;
        case RH.Polyline o:
          @base = PolylineToSpeckle(o) as Base;
          break;
        case NurbsCurve o:
          if (o.TryGetEllipse(out RH.Ellipse ellipse))
            @base = EllipseToSpeckle(ellipse);
          else
            @base = CurveToSpeckle(o) as Base;
          break;
        case PolylineCurve o:
          @base = PolylineToSpeckle(o);
          break;
        case PolyCurve o:
          @base = PolycurveToSpeckle(o);
          break;
        case RH.Box o:
          @base = BoxToSpeckle(o);
          break;
        case RH.Hatch o:
          @base = HatchToSpeckle(o);
          break;
        case RH.Mesh o:
          @base = MeshToSpeckle(o);
          break;
#if RHINO7
        case RH.SubD o:
          if (o.HasBrepForm)
            @base = BrepToSpeckle(o.ToBrep(new SubDToBrepOptions()));
          else
            @base = MeshToSpeckle(o);
          break;
#endif
        case RH.Extrusion o:
          @base = BrepToSpeckle(o);
          break;
        case RH.Brep o:
          @base = BrepToSpeckle(o.DuplicateBrep());
          break;
        case NurbsSurface o:
          @base = SurfaceToSpeckle(o);
          break;
        case ViewInfo o:
          @base = ViewToSpeckle(o);
          break;
        case InstanceDefinition o:
          @base = BlockDefinitionToSpeckle(o);
          break;
        case InstanceObject o:
          @base = BlockInstanceToSpeckle(o);
          break;
        case TextEntity o:
          @base = TextToSpeckle(o);
          break;
        default:
          throw new NotSupportedException();
      }

      if (material != null)
        @base["renderMaterial"] = material;

      if (style != null)
        @base["displayStyle"] = style;

      if (schema != null)
      {
        schema["renderMaterial"] = material;
        @base["@SpeckleSchema"] = schema;
      }

      return @base;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public Base ConvertToSpeckleBE(object @object)
    {
      // get schema if it exists
      RhinoObject obj = @object as RhinoObject;
      string schema = GetSchema(obj, out string[] args);

      if (obj is InstanceObject)
      {
        if (schema == "AdaptiveComponent")
            return InstanceToAdaptiveComponent(obj as InstanceObject, args);
        else
            throw new NotSupportedException();
      }

      switch (obj.Geometry)
      {
        case RH.Curve o:
          switch (schema)
          {
            case "Column":
              return CurveToSpeckleColumn(o);

            case "Beam":
              return CurveToSpeckleBeam(o);

            default:
              throw new NotSupportedException();
          }

        case RH.Brep o:
          switch (schema)
          {
            case "Floor":
              return BrepToSpeckleFloor(o);

            case "Roof":
              return BrepToSpeckleRoof(o);

            case "Wall":
              return BrepToSpeckleWall(o);

            case "FaceWall":
              return BrepToFaceWall(o, args);

            case "DirectShape":
              return BrepToDirectShape(o, args);

            default:
              throw new NotSupportedException();
          }

        case RH.Extrusion o:
          switch (schema)
          {
            case "FaceWall":
              return BrepToFaceWall(o.ToBrep(), args);

            case "DirectShape":
              return ExtrusionToDirectShape(o, args);

            default:
              throw new NotSupportedException();
          }

        case RH.Mesh o:
          switch (schema)
          {
            case "DirectShape":
              return MeshToDirectShape(o, args);

            default:
              throw new NotSupportedException();
          }

        default:
          throw new NotSupportedException();
      }
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

        case Pointcloud o:
          return PointcloudToNative(o);

        case Vector o:
          return VectorToNative(o);

        case Hatch o:
          return HatchToNative(o);

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
          return PolycurveToNative(o);

        case Curve o:
          return CurveToNative(o);

        case Box o:
          return BoxToNative(o);

        case Mesh o:
          return MeshToNative(o);

        case Brep o:
          // Brep conversion should always fallback to mesh if it fails.
          var b = BrepToNative(o);
          if (b == null)
            return (o.displayMesh != null) ? MeshToNative(o.displayMesh) : null;
          else
            return b;

        case Surface o:
          return SurfaceToNative(o);

        case Alignment o:
          return CurveToNative(o.baseCurve);

        case ModelCurve o:
          return CurveToNative(o.baseCurve);

        case DirectShape o:
          return (o.displayMesh != null) ? MeshToNative(o.displayMesh) : null;

        case View3D o:
          return ViewToNative(o);

        case BlockDefinition o:
          return BlockDefinitionToNative(o);

        case BlockInstance o:
          return BlockInstanceToNative(o);

        case Text o:
          return TextToNative(o);

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
      if (@object is RhinoObject ro && !(@object is InstanceObject))
      {
        @object = ro.Geometry;
      }

      switch (@object)
      {
        case Point3d _:
        case Rhino.Geometry.Point _:
        case PointCloud _:
        case Vector3d _:
        case RH.Interval _:
        case UVInterval _:
        case RH.Line _:
        case LineCurve _:
        case Rhino.Geometry.Hatch _:
        case RH.Plane _:
        case Rectangle3d _:
        case RH.Circle _:
        case RH.Arc _:
        case ArcCurve _:
        case RH.Ellipse _:
        case RH.Polyline _:
        case PolylineCurve _:
        case PolyCurve _:
        case NurbsCurve _:
        case RH.Box _:
        case RH.Mesh _:
#if RHINO7
case RH.SubD _:
#endif
        case RH.Extrusion _:
        case RH.Brep _:
        case NurbsSurface _:
          return true;
        // TODO: This types are not supported in GH!
        case ViewInfo _:
        case InstanceDefinition _:
        case InstanceObject _:
        case TextEntity _:
          return true;

        default:

          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _ :
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
        case Hatch _:
        case Box _:
        case Mesh _:
        case Brep _:
        case Surface _:
          return true;
        
        //TODO: This types are not supported in GH!
        case Pointcloud _:
        case ModelCurve _:
        case DirectShape _:
        case View3D _:
        case BlockDefinition _:
        case BlockInstance _:
        case Alignment _:
        case Text _:
          return true;
        
        default:
          return false;
      }
    }
  }
}