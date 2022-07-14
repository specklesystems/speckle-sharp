using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Display;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Dimension = Objects.Other.Dimension;
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
using RH = Rhino.Geometry;
using Spiral = Objects.Geometry.Spiral;
using Surface = Objects.Geometry.Surface;
using Transform = Objects.Other.Transform;
using Vector = Objects.Geometry.Vector;
using View3D = Objects.BuiltElements.View3D;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh : ISpeckleConverter
  {
#if RHINO6 && GRASSHOPPER
    public static string RhinoAppName = VersionedHostApplications.Grasshopper6;
#elif RHINO7 && GRASSHOPPER
    public static string RhinoAppName = VersionedHostApplications.Grasshopper7;
#elif RHINO6
    public static string RhinoAppName = VersionedHostApplications.Rhino6;
#elif RHINO7
    public static string RhinoAppName = VersionedHostApplications.Rhino7;
#endif

    public enum MeshSettings
    {
      Default,
      CurrentDoc
    }

    public MeshSettings SelectedMeshSettings = MeshSettings.Default;

    public ConverterRhinoGh()
    {
      var ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterRhinoGh)).GetName().Version;
      Report.Log($"Using converter: {Name} v{ver}");
    }
    public string Description => "Default Speckle Kit for Rhino & Grasshopper";
    public string Name => nameof(ConverterRhinoGh);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public ProgressReport Report { get; private set; } = new ProgressReport();

    public ReceiveMode ReceiveMode { get; set; }

    public IEnumerable<string> GetServicedApplications()
    {
      return new[] { RhinoAppName };
    }

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    public RhinoDoc Doc { get; private set; } = Rhino.RhinoDoc.ActiveDoc ?? null;

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();
    public void SetConverterSettings(object settings)
    {
      var s = (MeshSettings)settings;
      SelectedMeshSettings = s;
    }

    public void SetContextDocument(object doc)
    {
      Doc = (RhinoDoc)doc;
      Report.Log($"Using document: {Doc.Path}");
      Report.Log($"Using units: {ModelUnits}");
    }

    // speckle user string for custom schemas
    string SpeckleSchemaKey = "SpeckleSchema";

    public RH.Mesh GetRhinoRenderMesh(RhinoObject rhinoObj)
    {
      ObjRef[] meshObjRefs = RhinoObject.GetRenderMeshes(new List<RhinoObject>{rhinoObj}, false, false);
      if (meshObjRefs == null || meshObjRefs.Length == 0) return null;
      if (meshObjRefs.Length == 1) return meshObjRefs[0]?.Mesh();
      
      var joinedMesh = new RH.Mesh();
      foreach (var t in meshObjRefs)
      {
        var mesh = t?.Mesh();
        if (mesh != null)
          joinedMesh.Append(mesh);
      }

      return joinedMesh;
    }
    public Base ConvertToSpeckle(object @object)
    {
      RenderMaterial material = null;
      RH.Mesh displayMesh = null;
      DisplayStyle style = null;
      Base @base = null;
      Base schema = null;
      if (@object is RhinoObject ro)
      {
        material = RenderMaterialToSpeckle(ro.GetMaterial(true));
        style = DisplayStyleToSpeckle(ro.Attributes);

        if (ro.Attributes.GetUserString(SpeckleSchemaKey) != null) // schema check - this will change in the near future
          schema = ConvertToSpeckleBE(ro) ?? ConvertToSpeckleStr(ro);
        
        // Fast way to get the displayMesh, try to get the mesh rhino shows on the viewport when available.
        // This will only return a mesh if the object has been displayed in any mode other than Wireframe.
        if(ro is BrepObject || ro is ExtrusionObject)
          displayMesh = GetRhinoRenderMesh(ro);
        
        if (!(@object is InstanceObject)) // block instance check
          @object = ro.Geometry;
      }

      switch (@object)
      {
        case Point3d o:
          @base = PointToSpeckle(o);
          Report.Log($"Converted Point {o}");
          break;
        case Rhino.Geometry.Point o:
          @base = PointToSpeckle(o);
          Report.Log($"Converted Point {o}");
          break;
        case PointCloud o:
          @base = PointcloudToSpeckle(o);
          Report.Log($"Converted PointCloud");
          break;
        case Vector3d o:
          @base = VectorToSpeckle(o);
          Report.Log($"Converted Vector3d {o}");
          break;
        case RH.Interval o:
          @base = IntervalToSpeckle(o);
          Report.Log($"Converted Interval {o}");
          break;
        case UVInterval o:
          @base = Interval2dToSpeckle(o);
          Report.Log($"Converted Interval2d {o}");
          break;
        case RH.Line o:
          @base = LineToSpeckle(o);
          Report.Log($"Converted Line");
          break;
        case LineCurve o:
          @base = LineToSpeckle(o);
          Report.Log($"Converted LineCurve");
          break;
        case RH.Plane o:
          @base = PlaneToSpeckle(o);
          Report.Log($"Converted Plane");
          break;
        case Rectangle3d o:
          @base = PolylineToSpeckle(o);
          Report.Log($"Converted Polyline");
          break;
        case RH.Circle o:
          @base = CircleToSpeckle(o);
          Report.Log($"Converted Circle");
          break;
        case RH.Arc o:
          @base = ArcToSpeckle(o);
          Report.Log($"Converted Arc");
          break;
        case ArcCurve o:
          @base = ArcToSpeckle(o);
          Report.Log($"Converted Arc");
          break;
        case RH.Ellipse o:
          @base = EllipseToSpeckle(o);
          Report.Log($"Converted Ellipse");
          break;
        case RH.Polyline o:
          @base = PolylineToSpeckle(o) as Base;
          Report.Log($"Converted Polyline");
          break;
        case NurbsCurve o:
          if (o.TryGetEllipse(out RH.Ellipse ellipse))
          {
            @base = EllipseToSpeckle(ellipse);
            Report.Log($"Converted NurbsCurve as Ellipse");
          }
          else
          {
            @base = CurveToSpeckle(o) as Base;
            Report.Log($"Converted NurbsCurve");
          }
          break;
        case PolylineCurve o:
          @base = PolylineToSpeckle(o);
          Report.Log($"Converted Polyline Curve");
          break;
        case PolyCurve o:
          @base = PolycurveToSpeckle(o);
          Report.Log($"Converted PolyCurve");
          break;
        case RH.Box o:
          @base = BoxToSpeckle(o);
          Report.Log($"Converted Box");
          break;
        case RH.Hatch o:
          @base = HatchToSpeckle(o);
          Report.Log($"Converted Hatch");
          break;
        case RH.Mesh o:
          @base = MeshToSpeckle(o);
          Report.Log($"Converted Mesh");
          break;
        
# if GRASSHOPPER
        case RH.Transform o:
          @base = TransformToSpeckle(o);
          Report.Log("Converter Transform");
          break;
        case DisplayMaterial o:
          @base = DisplayMaterialToSpeckle(o);
          break;
#endif
        
#if RHINO7
        case RH.SubD o:
          if (o.HasBrepForm)
          {
            @base = BrepToSpeckle(o.ToBrep(new SubDToBrepOptions()),null, displayMesh);
            Report.Log($"Converted SubD as BREP");
          }
          else
          {
            @base = MeshToSpeckle(o);
            Report.Log($"Converted SubD as Mesh");
          }
          break;
#endif
        case RH.Extrusion o:
          @base = BrepToSpeckle(o.ToBrep(), null, displayMesh);
          Report.Log($"Converted Extrusion as Brep");
          break;
        case RH.Brep o:
          @base = BrepToSpeckle(o.DuplicateBrep(), null, displayMesh);
          Report.Log($"Converted Brep");
          break;
        case NurbsSurface o:
          @base = SurfaceToSpeckle(o);
          Report.Log($"Converted NurbsSurface");
          break;
        case ViewInfo o:
          @base = ViewToSpeckle(o);
          Report.Log($"Converted ViewInfo");
          break;
        case InstanceDefinition o:
          @base = BlockDefinitionToSpeckle(o);
          Report.Log($"Converted InstanceDefinition {o.Id}");
          break;
        case InstanceObject o:
          @base = BlockInstanceToSpeckle(o);
          Report.Log($"Converted BlockInstance {o.Id}");
          break;
        case TextEntity o:
          @base = TextToSpeckle(o);
          Report.Log($"Converted TextEntity");
          break;
        case Rhino.Geometry.Dimension o:
          @base = DimensionToSpeckle(o);
          Report.Log($"Converted Dimension");
          break;
        default:
          Report.Log($"Skipped not supported type: {@object.GetType()}");
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

      Base schemaBase = null;
      if (obj is InstanceObject)
      {
        if (schema == "AdaptiveComponent")
          schemaBase = InstanceToAdaptiveComponent(obj as InstanceObject, args);
        else
          Report.Log($"Skipping Instance conversion to unsupported schema {schema}");
      }

      switch (obj.Geometry)
      {
        case RH.Curve o:
          switch (schema)
          {
            case "Column":
              schemaBase = CurveToSpeckleColumn(o);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Beam":
              schemaBase = CurveToSpeckleBeam(o);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Duct":
              schemaBase = CurveToSpeckleDuct(o, args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Pipe":
              schemaBase = CurveToSpecklePipe(o, args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            default:
              Report.Log($"Skipping Curve conversion to schema {schema}");
              break;
          }
          break;

        case RH.Brep o:
          switch (schema)
          {
            case "Floor":
              schemaBase = BrepToSpeckleFloor(o);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Roof":
              schemaBase = BrepToSpeckleRoof(o);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Wall":
              schemaBase = BrepToSpeckleWall(o);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "FaceWall":
              schemaBase = BrepToFaceWall(o, args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "DirectShape":
              schemaBase = BrepToDirectShape(o, args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            default:
              Report.Log($"Skipping Brep Conversion to unsupported schema {schema}");
              break;
          }
          break;

        case RH.Extrusion o:
          switch (schema)
          {
            case "Floor":
              schemaBase = BrepToSpeckleFloor(o.ToBrep());
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Roof":
              schemaBase = BrepToSpeckleRoof(o.ToBrep());
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Wall":
              schemaBase = BrepToSpeckleWall(o.ToBrep());
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "FaceWall":
              schemaBase = BrepToFaceWall(o.ToBrep(), args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "DirectShape":
              schemaBase = ExtrusionToDirectShape(o, args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            default:
              Report.Log($"Skipping Extrusion conversion to unsupported schema {schema}");
              break;
          }
          break;

        case RH.Mesh o:
          switch (schema)
          {
            case "DirectShape":
              schemaBase = MeshToDirectShape(o, args);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            case "Topography":
              schemaBase = MeshToTopography(o);
              Report.Log($"Generated {schema} from {o.ObjectType}");
              break;

            default:
              Report.Log($"Skipping Mesh conversion to unsupported schema {schema}");
              break;
          }
          break;

        default:
          Report.Log($"{obj.ObjectType} is not supported in schema conversions.");
          break;
      }
      return schemaBase;
    }

    public List<Base> ConvertToSpeckleBE(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckleBE(x)).ToList();
    }

    public Base ConvertToSpeckleStr(object @object)
    {
      // get schema if it exists
      RhinoObject obj = @object as RhinoObject;
      string schema = GetSchema(obj, out string[] args);

      switch (obj.Geometry)
      {

        //case RH.Point o:
        //    switch (schema)
        //    {
        //        case "Node":
        //            return PointToSpeckleNode(o);

        //        default:
        //            throw new NotSupportedException();
        //    }

        //case RH.Curve o:
        //    switch (schema)
        //    {
        //        case "Element1D":
        //            return CurveToSpeckleElement1D(o);

        //        default:
        //            throw new NotSupportedException();
        //    }

        //case RH.Mesh o:
        //    switch (schema)
        //    {
        //        case "Element2D":
        //            return MeshToSpeckleElement2D(o);

        //    case "Element3D":
        //        return MeshToSpeckleElement3D(o);

        //            default:
        //            throw new NotSupportedException();
        //    }

        default:
          throw new NotSupportedException();
      }
    }

    public List<Base> ConvertToSpeckleStr(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckleStr(x)).ToList();
    }


    public object ConvertToNative(Base @object)
    {
      object rhinoObj = null;
      bool isFromRhino = @object[RhinoPropName] != null ? true : false;
      switch (@object)
      {
        case Point o:
          rhinoObj = PointToNative(o);
          Report.Log($"Created Point {o.id}");
          break;

        case Pointcloud o:
          rhinoObj = PointcloudToNative(o);
          Report.Log($"Created PointCloud {o.id}");
          break;

        case Vector o:
          rhinoObj = VectorToNative(o);
          Report.Log($"Created Vector {o.id}");
          break;

        case Hatch o:
          rhinoObj = HatchToNative(o);
          Report.Log($"Created Hatch {o.id}");
          break;

        case Interval o:
          rhinoObj = IntervalToNative(o);
          Report.Log($"Created Interval {o.id}");
          break;

        case Interval2d o:
          rhinoObj = Interval2dToNative(o);
          Report.Log($"Created Interval2d {o.id}");
          break;

        case Line o:
          rhinoObj = LineToNative(o);
          Report.Log($"Created Line {o.id}");
          break;

        case Plane o:
          rhinoObj = PlaneToNative(o);
          Report.Log($"Created Plane {o.id}");
          break;

        case Circle o:
          rhinoObj = CircleToNative(o);
          Report.Log($"Created Circle {o.id}");
          break;

        case Arc o:
          rhinoObj = ArcToNative(o);
          Report.Log($"Created Arc {o.id}");
          break;

        case Ellipse o:
          rhinoObj = EllipseToNative(o);
          Report.Log($"Created Ellipse {o.id}");
          break;

        case Spiral o:
          rhinoObj = SpiralToNative(o);
          Report.Log($"Created Spiral {o.id} as Curve");
          break;

        case Polyline o:
          rhinoObj = PolylineToNative(o);
          Report.Log($"Created Polyline {o.id}");
          break;

        case Polycurve o:
          rhinoObj = PolycurveToNative(o);
          Report.Log($"Created PolyCurve {o.id}");
          break;

        case Curve o:
          rhinoObj = CurveToNative(o);
          Report.Log($"Created Curve {o.id}");
          break;

        case Box o:
          rhinoObj = BoxToNative(o);
          Report.Log($"Created Box {o.id}");
          break;

        case Mesh o:
          rhinoObj = MeshToNative(o);
          Report.Log($"Created Mesh {o.id}");
          break;

        case Brep o:
          // Brep conversion should always fallback to mesh if it fails.
          var b = BrepToNative(o);
          if (b == null)
          {
            rhinoObj = o.displayValue?.Select(MeshToNative).ToArray();
            Report.Log($"Created Brep {o.id} as Meshes");
          }
          else
          {
            rhinoObj = b;
            Report.Log($"Created Brep {o.id}");
          }
          break;

        case Surface o:
          rhinoObj = SurfaceToNative(o);
          Report.Log($"Created Surface {o.id}");
          break;

        case Alignment o:
          if (o.curves is null) // TODO: remove after a few releases, this is for backwards compatibility
          {
            rhinoObj = CurveToNative(o.baseCurve);
            Report.Log($"Created Alignment {o.id}");
            break;
          }
          rhinoObj = AlignmentToNative(o);
          Report.Log($"Created Alignment {o.id} as Curve");
          break;

        case ModelCurve o:
          rhinoObj = CurveToNative(o.baseCurve);
          Report.Log($"Created ModelCurve {o.id}");
          break;

        case DirectShape o:
          rhinoObj = DirectShapeToNative(o);
          Report.Log($"Created DirectShape {o.id}");
          break;

        case View3D o:
          rhinoObj = ViewToNative(o);
          Report.Log($"Created View3D {o.id}");
          break;

        case BlockDefinition o:
          rhinoObj = BlockDefinitionToNative(o);
          Report.Log($"Created BlockDefinition {o.id}");
          break;

        case BlockInstance o:
          rhinoObj = BlockInstanceToNative(o);
          Report.Log($"Created BlockInstance {o.id}");
          break;

        case Text o:
          rhinoObj = TextToNative(o);
          Report.Log($"Created Text {o.id}");
          break;

        case Dimension o:
          rhinoObj = isFromRhino ? RhinoDimensionToNative(o) : DimensionToNative(o);
          Report.Log($"Created Dimension {o.id}");
          break;

        case Objects.Structural.Geometry.Element1D o:
          rhinoObj = element1DToNative(o);
          Report.Log($"Created Element1D with line {o.id}");
          break;

        case DisplayStyle o:
          rhinoObj = DisplayStyleToNative(o);
          break;

        case RenderMaterial o:
          #if GRASSHOPPER
            rhinoObj = RenderMaterialToDisplayMaterial(o);
          #else
            rhinoObj = RenderMaterialToNative(o);
          #endif
          break;
        case Transform o:
          rhinoObj = TransformToNative(o);
          break;
        default:
          Report.Log($"Skipped not supported type: {@object.GetType()} {@object.id}");
          throw new NotSupportedException();
      }

      return rhinoObj;
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
        
#if GRASSHOPPER
        // This types are ONLY supported in GH!
        case RH.Transform _:
        case DisplayMaterial _:
          return true;
#else
        // This types are NOT supported in GH!
        case ViewInfo _:
        case InstanceDefinition _:
        case InstanceObject _:
        case TextEntity _:
        case RH.Dimension _: 
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
        case Spiral _:
        case Polyline _:
        case Polycurve _:
        case Curve _:
        case Hatch _:
        case Box _:
        case Mesh _:
        case Brep _:
        case Surface _:
        case Structural.Geometry.Element1D _:
          return true;
#if GRASSHOPPER
        case Transform _:
        case RenderMaterial _:
          return true;
#else
        // This types are not supported in GH!
        case Pointcloud _:
        case DisplayStyle _:
        case ModelCurve _:
        case DirectShape _:
        case View3D _:
        case BlockDefinition _:
        case BlockInstance _:
        case Alignment _:
        case Text _:
        case Dimension _:
          return true;
#endif

        default:
          return false;
      }
    }
  }
}