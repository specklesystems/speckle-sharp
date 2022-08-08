using System;
using System.Collections.Generic;
using System.Linq;

using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Display;
using RH = Rhino.Geometry;
using Grasshopper.Kernel.Types;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Other;
using Objects.Primitive;

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

    public RhinoDoc Doc { get; private set; } = Rhino.RhinoDoc.ActiveDoc ?? null;

    public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();

    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();

    public void SetConverterSettings(object settings)
    {
      var s = (MeshSettings)settings;
      SelectedMeshSettings = s;
    }

    public void SetContextDocument(object doc)
    {
      Doc = (RhinoDoc)doc;
    }

    // speckle user string for custom schemas
    private string SpeckleSchemaKey = "SpeckleSchema";

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
      ObjectAttributes attributes = null;
      Base @base = null;
      Base schema = null;
      ApplicationObject reportObj = null;
      var notes = new List<string>();

      if (@object is RhinoObject ro)
      {
        reportObj = new ApplicationObject(ro.Id.ToString(), ro.ObjectType.ToString());
        material = RenderMaterialToSpeckle(ro.GetMaterial(true));
        style = DisplayStyleToSpeckle(ro.Attributes);

        // Fast way to get the displayMesh, try to get the mesh rhino shows on the viewport when available.
        // This will only return a mesh if the object has been displayed in any mode other than Wireframe.
        if (ro is BrepObject || ro is ExtrusionObject)
          displayMesh = GetRhinoRenderMesh(ro);

        if (ro.Attributes.GetUserString(SpeckleSchemaKey) != null) // schema check - this will change in the near future
          schema = ConvertToSpeckleBE(ro, reportObj, displayMesh) ?? ConvertToSpeckleStr(ro, reportObj);

        attributes = ro.Attributes;

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
            @base = BrepToSpeckle(o.ToBrep(new SubDToBrepOptions()),null, displayMesh);
          else
            @base = MeshToSpeckle(o);
          break;
#endif
        case RH.Extrusion o:
          @base = BrepToSpeckle(o.ToBrep(), null, displayMesh);
          break;
        case RH.Brep o:
          @base = BrepToSpeckle(o.DuplicateBrep(), null, displayMesh);
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
        case Rhino.Geometry.Dimension o:
          @base = DimensionToSpeckle(o);
          break;
        default:
          if (reportObj != null)
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"{@object.GetType()} type not supported");
            Report.UpdateReportObject(reportObj);
          }
          return @base;
      }

      if (@base is null) return @base;

      if (attributes != null)
        GetUserInfo(@base, attributes);
      if (material != null)
        @base["renderMaterial"] = material;
      if (style != null)
        @base["displayStyle"] = style;
      if (schema != null)
      {
        notes.Add($"Attached {schema.speckle_type} schema");
        schema["renderMaterial"] = material;
        @base["@SpeckleSchema"] = schema;
      }

      if (reportObj != null)
      {
        reportObj.Update(log: notes);
        Report.UpdateReportObject(reportObj);
      }

      return @base;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public Base ConvertToSpeckleBE(object @object, ApplicationObject reportObj, RH.Mesh displayMesh)
    {
      // get schema if it exists
      RhinoObject obj = @object as RhinoObject;
      string schema = GetSchema(obj, out string[] args);

      Base schemaBase = null;
      var notes = new List<string>();
      if (obj is InstanceObject)
      {
        if (schema == "AdaptiveComponent")
          schemaBase = InstanceToAdaptiveComponent(obj as InstanceObject, args);
        else
          reportObj.Update(logItem: $"Skipping Instance conversion to unsupported schema {schema}");
      }

      switch (obj.Geometry)
      {
        case RH.Curve o:
          switch (schema)
          {
            case "Column":
              schemaBase = CurveToSpeckleColumn(o);
              break;

            case "Beam":
              schemaBase = CurveToSpeckleBeam(o);
              break;

            case "Duct":
              schemaBase = CurveToSpeckleDuct(o, args, out notes);
              break;

            case "Pipe":
              schemaBase = CurveToSpecklePipe(o, args, out notes);
              break;

            default:
              reportObj.Update(logItem: $"{schema} creation from {o.ObjectType} is not supported");
              break;
          }
          break;

        case RH.Brep o:
          switch (schema)
          {
            case "Floor":
              schemaBase = BrepToSpeckleFloor(o, out notes);
              break;

            case "Roof":
              schemaBase = BrepToSpeckleRoof(o, out notes);
              break;

            case "Wall":
              schemaBase = BrepToSpeckleWall(o, out notes);
              break;

            case "FaceWall":
              schemaBase = BrepToFaceWall(o, args);
              break;

            case "DirectShape":
              schemaBase = BrepToDirectShape(o, args);
              break;

            case "Topography":
              schemaBase = displayMesh != null ? MeshToTopography(displayMesh) :  BrepToTopography(o);
              break;

            default:
              reportObj.Update(logItem: $"{schema} creation from {o.ObjectType} is not supported");
              break;
          }
          break;

        case RH.Extrusion o:
          switch (schema)
          {
            case "Floor":
              schemaBase = BrepToSpeckleFloor(o.ToBrep(), out notes);
              break;

            case "Roof":
              schemaBase = BrepToSpeckleRoof(o.ToBrep(), out notes);
              break;

            case "Wall":
              schemaBase = BrepToSpeckleWall(o.ToBrep(), out notes);
              break;

            case "FaceWall":
              schemaBase = BrepToFaceWall(o.ToBrep(), args);
              break;

            case "DirectShape":
              schemaBase = ExtrusionToDirectShape(o, args);
              break;
            
            case "Topography":
              schemaBase = displayMesh != null ? MeshToTopography(displayMesh) : MeshToTopography(o.GetMesh(MeshType.Default));
              break;

            default:
              reportObj.Update(logItem: $"{schema} creation from {o.ObjectType} is not supported");
              break;
          }
          break;

        case RH.Mesh o:
          switch (schema)
          {
            case "DirectShape":
              schemaBase = MeshToDirectShape(o, args);
              break;

            case "Topography":
              schemaBase = MeshToTopography(o);
              break;

            default:
              reportObj.Update(logItem: $"{schema} creation from {o.ObjectType} is not supported");
              break;
          }
          break;

#if RHINO7
        case RH.SubD o:
          if (o.HasBrepForm)
            schemaBase = displayMesh != null ? MeshToTopography(displayMesh) : BrepToTopography(o.ToBrep(new SubDToBrepOptions()));
          else
            schemaBase = MeshToTopography(o);
          break;
#endif

        default:
          reportObj.Update(logItem: $"{obj.ObjectType} is not supported in schema conversions.");
          break;
      }
      reportObj.Log.AddRange(notes);
      if (schemaBase == null)
        reportObj.Update(logItem: $"{schema} schema creation failed");
      return schemaBase;
    }

    public Base ConvertToSpeckleStr(object @object, ApplicationObject reportObj)
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

    public object ConvertToNative(Base @object)
    {
      object rhinoObj = null;
      bool isFromRhino = @object[RhinoPropName] != null ? true : false;
      var reportObj = Report.GetReportObject(@object.id, out int index) ? new ApplicationObject(@object.id, @object.speckle_type) : null;
      List<string> notes = new List<string>();
      switch (@object)
      {
        case Point o:
          rhinoObj = PointToNative(o);
          break;

        case Pointcloud o:
          rhinoObj = PointcloudToNative(o);
          break;

        case Vector o:
          rhinoObj = VectorToNative(o);
          break;

        case Hatch o:
          rhinoObj = HatchToNative(o);
          break;

        case Interval o:
          rhinoObj = IntervalToNative(o);
          break;

        case Interval2d o:
          rhinoObj = Interval2dToNative(o);
          break;

        case Line o:
          rhinoObj = LineToNative(o);
          break;

        case Plane o:
          rhinoObj = PlaneToNative(o);
          break;

        case Circle o:
          rhinoObj = CircleToNative(o);
          break;

        case Arc o:
          rhinoObj = ArcToNative(o);
          break;

        case Ellipse o:
          rhinoObj = EllipseToNative(o);
          break;

        case Spiral o:
          rhinoObj = SpiralToNative(o);
          break;

        case Polyline o:
          rhinoObj = PolylineToNative(o);
          break;

        case Polycurve o:
          rhinoObj = PolycurveToNative(o);
          break;

        case Curve o:
          rhinoObj = CurveToNative(o);
          break;

        case Box o:
          rhinoObj = BoxToNative(o);
          break;

        case Mesh o:
          rhinoObj = MeshToNative(o);
          break;

        case Brep o:
          // Brep conversion should always fallback to mesh if it fails.
          var b = BrepToNative(o, out notes);
          if (b == null)
          {
            notes.Add($"{b.ObjectType} conversion failed: converting displayValue as Meshes");
            rhinoObj = o.displayValue?.Select(MeshToNative).ToArray();
          }
          else
          {
            rhinoObj = b;
          }
          break;

        case Surface o:
          rhinoObj = SurfaceToNative(o);
          break;

        case Alignment o:
          if (o.curves is null) // TODO: remove after a few releases, this is for backwards compatibility
          {
            rhinoObj = CurveToNative(o.baseCurve);
            break;
          }
          rhinoObj = AlignmentToNative(o);
          break;

        case ModelCurve o:
          rhinoObj = CurveToNative(o.baseCurve);
          break;

        case DirectShape o:
          rhinoObj = DirectShapeToNative(o, out notes);
          break;

        case View3D o:
          rhinoObj = ViewToNative(o);
          break;

        case BlockDefinition o:
          rhinoObj = BlockDefinitionToNative(o, out notes);
          break;

        case BlockInstance o:
          rhinoObj = BlockInstanceToNative(o, out notes);
          break;

        case Text o:
          rhinoObj = TextToNative(o);
          break;

        case Dimension o:
          rhinoObj = isFromRhino ? RhinoDimensionToNative(o) : DimensionToNative(o);
          Report.Log($"Created Dimension {o.id}");
          break;

        case Objects.Structural.Geometry.Element1D o:
          rhinoObj = element1DToNative(o);
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
          if (reportObj != null)
          {
            reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"{@object.GetType()} type not supported");
            Report.UpdateReportObject(reportObj);
          }
          break;
      }

      if (reportObj != null)
      {
        reportObj.Update(log: notes);
        Report.UpdateReportObject(reportObj);
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