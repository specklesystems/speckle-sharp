#if GRASSHOPPER
using Grasshopper.Kernel.Types;
#endif
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.Curve;
using Objects.Geometry;
using Objects.Organization;
using Objects.Other;
using Objects.Primitive;
using Objects.Structural.Geometry;
using Rhino;
using Rhino.Collections;
using Rhino.Display;
using Rhino.DocObjects;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Plane = Objects.Geometry.Plane;
using RH = Rhino.Geometry;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh : ISpeckleConverter
{
#if RHINO6 && GRASSHOPPER
    public static string RhinoAppName = HostApplications.Grasshopper.GetVersion(HostAppVersion.v6);
#elif RHINO7 && GRASSHOPPER
  public static string RhinoAppName = HostApplications.Grasshopper.GetVersion(HostAppVersion.v7);
#elif RHINO6
  public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v6);
#elif RHINO7
    public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
#endif

  public enum MeshSettings
  {
    Default,
    CurrentDoc
  }

  public MeshSettings SelectedMeshSettings = MeshSettings.Default;

  public bool PreprocessGeometry;

  public ConverterRhinoGh()
  {
    var ver = Assembly.GetAssembly(typeof(ConverterRhinoGh)).GetName().Version;
  }

  public string Description => "Default Speckle Kit for Rhino & Grasshopper";
  public string Name => nameof(ConverterRhinoGh);
  public string Author => "Speckle";
  public string WebsiteOrEmail => "https://speckle.systems";

  public ProgressReport Report { get; private set; } = new();

  public ReceiveMode ReceiveMode { get; set; }

  public IEnumerable<string> GetServicedApplications()
  {
    return new[] { RhinoAppName };
  }

  public RhinoDoc Doc { get; private set; }

  public Dictionary<string, BlockDefinition> BlockDefinitions { get; private set; } = new();
  public Dictionary<string, InstanceDefinition> InstanceDefinitions { get; private set; } = new();

  public List<ApplicationObject> ContextObjects { get; set; } = new();

  public void SetContextObjects(List<ApplicationObject> objects)
  {
    ContextObjects = objects;
  }

  public void SetPreviousContextObjects(List<ApplicationObject> objects)
  {
    throw new NotImplementedException();
  }

  public void SetConverterSettings(object settings)
  {
    if (settings is Dictionary<string, object> dict)
    {
      if (dict.ContainsKey("meshSettings"))
        SelectedMeshSettings = (MeshSettings)dict["meshSettings"];

      if (dict.ContainsKey("preprocessGeometry"))
        PreprocessGeometry = (bool)dict["preprocessGeometry"];
      return;
    }

    // Keep this for backwards compatibility.
    var s = (MeshSettings)settings;
    SelectedMeshSettings = s;
  }

  public void SetContextDocument(object doc)
  {
    Doc = (RhinoDoc)doc;
  }

  // speckle user string for custom schemas
  private string SpeckleSchemaKey = "SpeckleSchema";
  private string SpeckleMappingKey = "SpeckleMapping";
  private string ApplicationIdKey = "applicationId";

  public RH.Mesh GetRhinoRenderMesh(RhinoObject rhinoObj)
  {
    ObjRef[] meshObjRefs = RhinoObject.GetRenderMeshes(new List<RhinoObject> { rhinoObj }, false, false);
    if (meshObjRefs == null || meshObjRefs.Length == 0)
      return null;
    if (meshObjRefs.Length == 1)
      return meshObjRefs[0]?.Mesh();

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
    ApplicationObject reportObj = null;
    RenderMaterial material = null;
    DisplayStyle style = null;
    RH.Mesh displayMesh = null;
    ObjectAttributes attributes = null;
    ArchivableDictionary userDictionary = null;
    NameValueCollection userStrings = null;
    string objName = null;

    Base @base = null;
    Base schema = null;
    var notes = new List<string>();
    var defaultPreprocess = PreprocessGeometry;

    try
    {
      switch (@object)
      {
        case RhinoObject ro:
          var roId = ro.Attributes.GetUserString(ApplicationIdKey) ?? ro.Id.ToString();
          reportObj = new ApplicationObject(ro.Id.ToString(), ro.ObjectType.ToString()) { applicationId = roId };
          material = RenderMaterialToSpeckle(ro.GetMaterial(true));
          style = DisplayStyleToSpeckle(ro.Attributes);
          userDictionary = ro.UserDictionary;
          userStrings = ro.Attributes.GetUserStrings();
          objName = ro.Attributes.Name;

          // Fast way to get the displayMesh, try to get the mesh rhino shows on the viewport when available.
          // This will only return a mesh if the object has been displayed in any mode other than Wireframe.
          if (ro is BrepObject || ro is ExtrusionObject)
            displayMesh = GetRhinoRenderMesh(ro);

          //rhino BIM to be deprecated after the mapping tool is released
          if (ro.Attributes.GetUserString(SpeckleSchemaKey) != null) // schema check - this will change in the near future
            schema = ConvertToSpeckleBE(ro, reportObj, displayMesh) ?? ConvertToSpeckleStr(ro, reportObj);

          //mapping tool
          var mappingString = ro.Attributes.GetUserString(SpeckleMappingKey);
          if (mappingString != null)
            schema = MappingToSpeckle(mappingString, ro, notes);

          if (!(@object is InstanceObject))
            @object = ro.Geometry; // block instance check
          break;

        case Layer l:
          var lId = l.GetUserString(ApplicationIdKey) ?? l.Id.ToString();
          reportObj = new ApplicationObject(l.Id.ToString(), "Layer") { applicationId = lId };
          if (l.RenderMaterial != null)
            material = RenderMaterialToSpeckle(l.RenderMaterial.SimulateMaterial(true));
          style = DisplayStyleToSpeckle(new ObjectAttributes(), l);
          userDictionary = l.UserDictionary;
          userStrings = l.GetUserStrings();
          break;
      }

      if (schema != null)
        PreprocessGeometry = true;

      switch (@object)
      {
        case RhinoDoc doc: // this is the base commit! Create a collection object to use
          @base = CollectionToSpeckle(doc);
          break;

        case RH.Point3d o:
          @base = PointToSpeckle(o);
          break;
        case RH.Point o:
          @base = PointToSpeckle(o);
          break;
        case RH.PointCloud o:
          @base = PointcloudToSpeckle(o);
          break;
        case RH.Vector3d o:
          @base = VectorToSpeckle(o);
          break;
        case RH.Interval o:
          @base = IntervalToSpeckle(o);
          break;
        case RH.Line o:
          @base = LineToSpeckle(o);
          break;
        case RH.LineCurve o:
          @base = LineToSpeckle(o);
          break;
        case RH.Plane o:
          @base = PlaneToSpeckle(o);
          break;
        case RH.Rectangle3d o:
          @base = PolylineToSpeckle(o);
          break;
        case RH.Circle o:
          @base = CircleToSpeckle(o);
          break;
        case RH.Arc o:
          @base = ArcToSpeckle(o);
          break;
        case RH.ArcCurve o:
          @base = ArcToSpeckle(o);
          break;
        case RH.Ellipse o:
          @base = EllipseToSpeckle(o);
          break;
        case RH.Polyline o:
          @base = PolylineToSpeckle(o) as Base;
          break;
        case RH.NurbsCurve o:
          @base = CurveToSpeckle(o) as Base;
          break;
        case RH.PolylineCurve o:
          @base = PolylineToSpeckle(o);
          break;
        case RH.PolyCurve o:
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

#if GRASSHOPPER
        case RH.Transform o:
          @base = new Transform(o.ToFloatArray(true), ModelUnits);
          break;
        case DisplayMaterial o:
          @base = DisplayMaterialToSpeckle(o);
          break;
        case UVInterval o:
          @base = Interval2dToSpeckle(o);
          break;
#endif

#if RHINO7
        case RH.SubD o:
          if (o.HasBrepForm)
            @base = BrepToSpeckle(o.ToBrep(new RH.SubDToBrepOptions()), null, displayMesh, material);
          else
            @base = MeshToSpeckle(o);
          break;
#endif
        case RH.Extrusion o:
          @base = BrepToSpeckle(o.ToBrep(), null, displayMesh, material);
          break;
        case RH.Brep o:
          @base = BrepToSpeckle(o.DuplicateBrep(), null, displayMesh, material);
          break;
        case RH.NurbsSurface o:
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
        case RH.TextEntity o:
          @base = TextToSpeckle(o);
          break;
        case RH.Dimension o:
          @base = DimensionToSpeckle(o);
          break;
        case Layer o:
          @base = LayerToSpeckle(o);
          break;
        default:
          if (reportObj != null)
          {
            reportObj.Update(
              status: ApplicationObject.State.Skipped,
              logItem: $"{@object.GetType()} type not supported"
            );
            Report.UpdateReportObject(reportObj);
          }

          return @base;
      }

      if (@base is null)
        return @base;

      GetUserInfo(@base, out List<string> attributeNotes, userDictionary, userStrings, objName);
      notes.AddRange(attributeNotes);
      if (material != null)
        @base["renderMaterial"] = material;
      if (style != null)
        @base["displayStyle"] = style;
      if (schema != null)
      {
        schema["renderMaterial"] = material;
        @base["@SpeckleSchema"] = schema;
      }
    }
    catch (Exception ex)
    {
      reportObj?.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"{@object.GetType()} unhandled conversion error: {ex.Message}\n{ex.StackTrace}"
      );
    }

    PreprocessGeometry = defaultPreprocess;

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

  private Base MappingToSpeckle(string mapping, RhinoObject @object, List<string> notes)
  {
    var defaultPreprocess = PreprocessGeometry;
    PreprocessGeometry = true;
    Base schemaObject = Operations.Deserialize(mapping);
    try
    {
      switch (schemaObject)
      {
        case RevitProfileWall o:
          var profileWallBrep = @object.Geometry is RH.Brep profileB
            ? profileB
            : ((RH.Extrusion)@object.Geometry)?.ToBrep();
          if (profileWallBrep == null)
          {
            throw new ArgumentException("Wall geometry can only be a brep or extrusion");
          }

          var edges = profileWallBrep.DuplicateNakedEdgeCurves(true, false);
          var profileCurve = RH.Curve.JoinCurves(edges);
          if (profileCurve.Count() != 1)
          {
            throw new Exception("Surface external edges should be joined into 1 curve");
          }

          var speckleProfileCurve = CurveToSpeckle(profileCurve.First());
          var profile = new Polycurve()
          {
            segments = new List<ICurve>() { speckleProfileCurve },
            length = profileCurve.First().GetLength(),
            closed = profileCurve.First().IsClosed,
            units = ModelUnits
          };
          o.profile = profile;
          break;

        case RevitFaceWall o:
          var faceWallBrep = @object.Geometry is RH.Brep faceB ? faceB : ((RH.Extrusion)@object.Geometry)?.ToBrep();
          o.brep = BrepToSpeckle(faceWallBrep);
          break;

        //NOTE: this works for BOTH the Wall.cs class and RevitWall.cs class etc :)
        case Wall o:
          var extrusion = (RH.Extrusion)@object.Geometry;
          var bottomCrv = extrusion.Profile3d(new RH.ComponentIndex(RH.ComponentIndexType.ExtrusionBottomProfile, 0));
          var topCrv = extrusion.Profile3d(new RH.ComponentIndex(RH.ComponentIndexType.ExtrusionTopProfile, 0));
          var height = topCrv.PointAtStart.Z - bottomCrv.PointAtStart.Z;
          o.height = height;
          o.baseLine = CurveToSpeckle(bottomCrv);
          break;

        case Floor o:
          var brep = (RH.Brep)@object.Geometry;
          var extCurves = GetSurfaceBrepEdges(brep); // extract outline
          var intCurves = GetSurfaceBrepEdges(brep, getInterior: true); // extract voids
          o.outline = extCurves.First();
          o.voids = intCurves;
          break;

        case Beam o:
          o.baseLine = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Brace o:
          o.baseLine = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Column o:
          o.baseLine = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Pipe o:
          o.baseCurve = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case Duct o:
          o.baseCurve = CurveToSpeckle((RH.Curve)@object.Geometry);
          break;

        case RevitTopography o:
          o.baseGeometry = MeshToSpeckle((RH.Mesh)@object.Geometry);
          break;

        case DirectShape o:
          if (string.IsNullOrEmpty(o.name))
            o.name = "Speckle Mapper Shape";
          if (@object.Geometry as RH.Brep != null)
            o.baseGeometries = new List<Base> { BrepToSpeckle((RH.Brep)@object.Geometry) };
          else if (@object.Geometry as RH.Mesh != null)
            o.baseGeometries = new List<Base> { MeshToSpeckle((RH.Mesh)@object.Geometry) };
          break;

        case FreeformElement o:
          if (@object.Geometry as RH.Brep != null)
            o.baseGeometries = new List<Base> { BrepToSpeckle((RH.Brep)@object.Geometry) };
          else if (@object.Geometry as RH.Mesh != null)
            o.baseGeometries = new List<Base> { MeshToSpeckle((RH.Mesh)@object.Geometry) };
          break;

        case FamilyInstance o:
          if (@object.Geometry is RH.Point p)
          {
            o.basePoint = PointToSpeckle(p);
          }
          else if (@object is InstanceObject)
          {
            var block = BlockInstanceToSpeckle(@object as InstanceObject);
            o.basePoint = block.GetInsertionPlane().origin;
            block.transform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector4 translation);
            o.rotation = Math.Acos(rotation.W) * 2;
          }

          break;
      }

      schemaObject.applicationId = @object.Id.ToString();
      schemaObject["units"] = ModelUnits;

      notes.Add($"Attached {schemaObject.speckle_type} schema");
    }
    catch (Exception ex)
    {
      notes.Add($"Could not attach {schemaObject.speckle_type} schema: {ex.Message}");
    }

    PreprocessGeometry = defaultPreprocess;
    return schemaObject;
  }

  public Base ConvertToSpeckleBE(object @object, ApplicationObject reportObj, RH.Mesh displayMesh)
  {
    var defaultPreprocess = PreprocessGeometry;
    PreprocessGeometry = true;
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
            schemaBase = displayMesh != null ? MeshToTopography(displayMesh) : BrepToTopography(o);
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
            schemaBase =
              displayMesh != null ? MeshToTopography(displayMesh) : MeshToTopography(o.GetMesh(RH.MeshType.Default));
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
          schemaBase = displayMesh != null
            ? MeshToTopography(displayMesh)
            : BrepToTopography(o.ToBrep(new RH.SubDToBrepOptions()));
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
    PreprocessGeometry = defaultPreprocess;
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
    var reportObj =
      @object.id != null && Report.ReportObjects.ContainsKey(@object.id)
        ? new ApplicationObject(@object.id, @object.speckle_type)
        : null;
    List<string> notes = new();
    try
    {
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
#if GRASSHOPPER
        case Interval2d o:
          rhinoObj = Interval2dToNative(o);
          break;
#endif
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
          rhinoObj = DefinitionToNative(o, out notes);
          break;

        case Instance o:
          rhinoObj = InstanceToNative(o);
          break;

        case Text o:
          rhinoObj = TextToNative(o);
          break;

        case Dimension o:
          rhinoObj = isFromRhino ? RhinoDimensionToNative(o) : DimensionToNative(o);
          break;

        case Element1D o:
          rhinoObj = element1DToNative(o);
          break;

        case Collection o:
          rhinoObj = CollectionToNative(o);
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
            reportObj.Update(
              status: ApplicationObject.State.Skipped,
              logItem: $"{@object.GetType()} type not supported"
            );
            Report.UpdateReportObject(reportObj);
          }

          break;
      }
    }
    catch (Exception ex)
    {
      reportObj.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"{@object.GetType()} unhandled converion error: {ex.Message}\n{ex.StackTrace}"
      );
    }

    switch (rhinoObj)
    {
      case ApplicationObject o: // some to native methods return an application object (if object is baked to doc during conv)
        rhinoObj = o.Converted.Any() ? o.Converted : null;
        if (reportObj != null)
          reportObj.Update(
            status: o.Status,
            createdIds: o.CreatedIds,
            converted: o.Converted,
            container: o.Container,
            log: o.Log
          );
        break;
      default:
        if (reportObj != null)
          reportObj.Update(log: notes);
        break;
    }

    if (reportObj != null)
      Report.UpdateReportObject(reportObj);
    return rhinoObj;
  }

  public List<object> ConvertToNative(List<Base> objects)
  {
    return objects.Select(x => ConvertToNative(x)).ToList();
  }

  public bool CanConvertToSpeckle(object @object)
  {
    if (@object is RhinoObject ro && !(@object is InstanceObject))
      @object = ro.Geometry;

    switch (@object)
    {
      case RH.Point3d _:
      case RH.Point _:
      case RH.PointCloud _:
      case RH.Vector3d _:
      case RH.Interval _:
      case RH.Line _:
      case RH.LineCurve _:
      case RH.Hatch _:
      case RH.Plane _:
      case RH.Rectangle3d _:
      case RH.Circle _:
      case RH.Arc _:
      case RH.ArcCurve _:
      case RH.Ellipse _:
      case RH.Polyline _:
      case RH.PolylineCurve _:
      case RH.PolyCurve _:
      case RH.NurbsCurve _:
      case RH.Box _:
      case RH.Mesh _:
#if RHINO7
      case RH.SubD _:
#endif
      case RH.Extrusion _:
      case RH.Brep _:
      case RH.NurbsSurface _:
        return true;

#if GRASSHOPPER
      // This types are ONLY supported in GH!
      case RH.Transform _:
      case DisplayMaterial _:
      case UVInterval _:
        return true;
#else
      // This types are NOT supported in GH!
      case ViewInfo _:
      case InstanceDefinition _:
      case InstanceObject _:
      case RH.TextEntity _:
      case RH.Dimension _:
      case Layer _:
        return true;
#endif
      default:
        return false;
    }
  }

  public bool CanConvertToNative_old(Base @object)
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
      case Element1D _:
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

  /// <summary>
  /// Indicates if a Speckle object should be converted to a top-level Rhino document object
  /// </summary>
  /// <param name="object"></param>
  /// <returns>True if the Speckle object should be converted, false if not</returns>
  /// <remarks>Objects like Planes, Vectors, RenderMaterials, and DisplayStyles can be converted to Rhino native equivalents but not added to the document as top-level objects</remarks>
  public bool CanConvertToNative(Base @object)
  {
    switch (@object)
    {
      case Point _:
      case Line _:
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
      case Element1D _:
        return true;
#if GRASSHOPPER
      case Interval _:
      case Interval2d _:
      case Plane _:
      case RenderMaterial _:
      case Spiral _:
      case Transform _:
      case Vector _:
        return true;
#else
      // This types are not supported in GH!
      case Pointcloud _:
      case Collection _:
      case ModelCurve _:
      case DirectShape _:
      case View3D _:
      case Instance _:
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
