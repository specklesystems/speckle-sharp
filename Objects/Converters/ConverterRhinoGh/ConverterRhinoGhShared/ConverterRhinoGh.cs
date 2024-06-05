using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.BuiltElements.Revit.Curve;
using Objects.Geometry;
using Objects.GIS;
using Objects.Other;
using Objects.Primitive;
using Objects.Structural.Geometry;
using Rhino;
using Rhino.Collections;

using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Plane = Objects.Geometry.Plane;
using RH = Rhino.Geometry;
using Vector = Objects.Geometry.Vector;

#if GRASSHOPPER
using Grasshopper.Kernel.Types;
using Rhino.Display;
#endif

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh : ISpeckleConverter
{
#if RHINO6 && GRASSHOPPER
    public static string RhinoAppName = HostApplications.Grasshopper.GetVersion(HostAppVersion.v6);
#elif RHINO7 && GRASSHOPPER
  public static string RhinoAppName = HostApplications.Grasshopper.GetVersion(HostAppVersion.v7);
#elif RHINO8 && GRASSHOPPER
  public static string RhinoAppName = HostApplications.Grasshopper.GetVersion(HostAppVersion.v8);
#elif RHINO6
  public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v6);
#elif RHINO7
    public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
#elif RHINO8
  public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v8);
#endif

  [Obsolete]
  public enum MeshSettings
  {
    Default,
    CurrentDoc
  }

  [Obsolete]
  public MeshSettings SelectedMeshSettings = MeshSettings.Default;

  public bool PreprocessGeometry;

  public Dictionary<string, string> Settings { get; private set; } = new Dictionary<string, string>();

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
    if (settings is Dictionary<string, string> temp)
    {
      Settings = temp;
    }
    // TODO: Both settings bellow are here for backwards compatibility and should be removed after consolidating settings
    else if (settings is MeshSettings meshSettings)
    {
      SelectedMeshSettings = meshSettings;
    }

    if (Settings.TryGetValue("preprocessGeometry", out string setting))
    {
      _ = bool.TryParse(setting, out PreprocessGeometry);
    }
  }

  public void SetContextDocument(object doc)
  {
    Doc = (RhinoDoc)doc;
  }

  // speckle user string for custom schemas
  private string SpeckleMappingKey = "SpeckleMapping";
  private string ApplicationIdKey = "applicationId";

  public RH.Mesh GetRhinoRenderMesh(RhinoObject rhinoObj)
  {
    ObjRef[] meshObjRefs = RhinoObject.GetRenderMeshes(new List<RhinoObject> { rhinoObj }, false, false);
    if (meshObjRefs == null || meshObjRefs.Length == 0)
    {
      return null;
    }

    if (meshObjRefs.Length == 1)
    {
      return meshObjRefs[0]?.Mesh();
    }

    var joinedMesh = new RH.Mesh();
    foreach (var t in meshObjRefs)
    {
      var mesh = t?.Mesh();
      if (mesh != null)
      {
        joinedMesh.Append(mesh);
      }
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

    // get preprocessing setting
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
          {
            displayMesh = GetRhinoRenderMesh(ro);
          }

          //mapping tool
          var mappingString = ro.Attributes.GetUserString(SpeckleMappingKey);
          if (mappingString != null)
          {
            schema = MappingToSpeckle(mappingString, ro, notes);
          }

          if (!(@object is InstanceObject))
          {
            @object = ro.Geometry; // block instance check
          }

          break;

        case Layer l:
          var lId = l.GetUserString(ApplicationIdKey) ?? l.Id.ToString();
          reportObj = new ApplicationObject(l.Id.ToString(), "Layer") { applicationId = lId };
          if (l.RenderMaterial != null)
          {
            material = RenderMaterialToSpeckle(l.RenderMaterial);
          }

          style = DisplayStyleToSpeckle(new ObjectAttributes(), l);
          userDictionary = l.UserDictionary;
          userStrings = l.GetUserStrings();
          break;
      }

      if (schema != null)
      {
        PreprocessGeometry = true;
      }

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

#if RHINO7_OR_GREATER
        case RH.SubD o:
          if (o.HasBrepForm)
          {
            @base = BrepToSpeckle(o.ToBrep(new RH.SubDToBrepOptions()), null, displayMesh, material);
          }
          else
          {
            @base = MeshToSpeckle(o);
          }

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
          throw new ConversionNotSupportedException(
            $"Rhino object of type {@object.GetType()} is not supported for conversion."
          );
      }

      if (@base is null)
      {
        return @base;
      }

      GetUserInfo(@base, out List<string> attributeNotes, userDictionary, userStrings, objName);
      notes.AddRange(attributeNotes);
      if (material != null)
      {
        @base["renderMaterial"] = material;
        if (schema != null)
        {
          schema["renderMaterial"] = material;
        }
      }
      if (style != null)
      {
        @base["displayStyle"] = style;
      }

      if (schema != null)
      {
        @base["@SpeckleSchema"] = schema;
      }
    }
    catch (ConversionNotSupportedException e)
    {
      SpeckleLog.Logger.Information(e, "{exceptionMessage}");
      reportObj?.Update(status: ApplicationObject.State.Skipped, logItem: e.Message);
    }
    catch (SpeckleException e)
    {
      SpeckleLog.Logger.Warning(e, "{exceptionMessage}");
      reportObj?.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"{@object.GetType()} unhandled conversion error: {e.Message}\n{e.StackTrace}"
      );
    }
    catch (Exception ex) when (!ex.IsFatal())
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

        case GridLine o:
          rhinoObj = GridlineToNative(o);
          break;

        case Alignment o:
          rhinoObj = AlignmentToNative(o);
          break;

        case PolygonElement o:
          rhinoObj = PolygonElementToNative(o);
          break;

        case GisFeature o:
          rhinoObj = GisFeatureToNative(o);
          break;

        case Level o:
          rhinoObj = LevelToNative(o);
          break;

        case ModelCurve o:
          rhinoObj = CurveToNative(o.baseCurve);
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

        case Parameter o:
          rhinoObj = ParameterToNative(o);
          break;

        default:
          throw new ConversionNotSupportedException(
            $"Speckle object of type {@object.GetType()} is not supported for conversion."
          );
      }
    }
    catch (ConversionNotSupportedException e)
    {
      SpeckleLog.Logger.Information(e, "{exceptionMessage}");
      reportObj?.Update(status: ApplicationObject.State.Skipped, logItem: e.Message);
    }
    catch (SpeckleException e)
    {
      SpeckleLog.Logger.Warning(e, "{exceptionMessage}");
      reportObj?.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"{@object.GetType()} unhandled conversion error: {e.Message}\n{e.StackTrace}"
      );
    }
    catch (Exception e) when (!e.IsFatal())
    {
      reportObj?.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"{@object.GetType()} unhandled conversion error: {e.Message}\n{e.StackTrace}"
      );
    }

    switch (rhinoObj)
    {
      case ApplicationObject o: // some to native methods return an application object (if object is baked to doc during conv)
        rhinoObj = o.Converted.Count == 0 ? null : o.Converted;
        reportObj?.Update(
          status: o.Status,
          createdIds: o.CreatedIds,
          converted: o.Converted,
          container: o.Container,
          log: o.Log
        );

        break;

      default:
        reportObj?.Update(log: notes);

        break;
    }

    if (reportObj != null)
    {
      Report.UpdateReportObject(reportObj);
    }

    return rhinoObj;
  }

  public object ConvertToNativeDisplayable(Base @object)
  {
    throw new NotImplementedException();
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
#if RHINO7_OR_GREATER
      case RH.SubD _:
#endif
      case RH.Extrusion _:
      case RH.Brep _:
      case RH.NurbsSurface _:
      case RH.TextEntity _:
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
      case RH.Dimension _:
      case Layer _:
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
      case Text _:
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
      case ModelCurve _:
      case View3D _:
      case Instance _:
      case GridLine _:
      case Alignment _:
      case PolygonElement _:
      case GisFeature _:
      case Level _:
      case Dimension _:
      case Collection c when !c.collectionType.ToLower().Contains("model"):
        return true;
#endif

      default:
        return false;
    }
  }

  public bool CanConvertToNativeDisplayable(Base @object)
  {
    return false;
  }
}
