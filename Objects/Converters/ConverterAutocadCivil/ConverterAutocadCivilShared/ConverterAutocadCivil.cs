using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Objects.BuiltElements;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Acad = Autodesk.AutoCAD;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using Alignment = Objects.BuiltElements.Alignment;
using Arc = Objects.Geometry.Arc;
using BlockDefinition = Objects.Other.BlockDefinition;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Dimension = Objects.Other.Dimension;
using Ellipse = Objects.Geometry.Ellipse;
using Hatch = Objects.Other.Hatch;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using ModelCurve = Objects.BuiltElements.Revit.Curve.ModelCurve;
using Point = Objects.Geometry.Point;
using Polycurve = Objects.Geometry.Polycurve;
using Polyline = Objects.Geometry.Polyline;
using Spiral = Objects.Geometry.Spiral;
using Speckle.Core.Logging;

#if CIVIL
using CivilDB = Autodesk.Civil.DatabaseServices;
#endif

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil : ISpeckleConverter
{
#if AUTOCAD2021
  public static string AutocadAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2021);
#elif AUTOCAD2022
  public static string AutocadAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2022);
#elif AUTOCAD2023
  public static string AutocadAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2023);
#elif AUTOCAD2024
  public static string AutocadAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2024);
#elif AUTOCAD2025
  public static string AutocadAppName = HostApplications.AutoCAD.GetVersion(HostAppVersion.v2025);
#elif CIVIL2021
  public static string AutocadAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2021);
#elif CIVIL2022
  public static string AutocadAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2022);
#elif CIVIL2023
  public static string AutocadAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2023);
#elif CIVIL2024
  public static string AutocadAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2024);
#elif CIVIL2025
  public static string AutocadAppName = HostApplications.Civil.GetVersion(HostAppVersion.v2025);
#elif ADVANCESTEEL2023
  public static string AutocadAppName = HostApplications.AdvanceSteel.GetVersion(HostAppVersion.v2023);
#elif ADVANCESTEEL2024
  public static string AutocadAppName = HostApplications.AdvanceSteel.GetVersion(HostAppVersion.v2024);
#endif

  public ConverterAutocadCivil()
  {
    var ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterAutocadCivil)).GetName().Version;
  }

  #region ISpeckleConverter props
  public string Description => "Default Speckle Kit for AutoCAD";
  public string Name => nameof(ConverterAutocadCivil);
  public string Author => "Speckle";
  public string WebsiteOrEmail => "https://speckle.systems";
  public ProgressReport Report { get; private set; } = new ProgressReport();

  public IEnumerable<string> GetServicedApplications() => new string[] { AutocadAppName };

  public Document Doc { get; private set; }
  public Transaction Trans { get; private set; } // TODO: evaluate if this should be here
  public Dictionary<string, string> Settings { get; private set; } = new Dictionary<string, string>();
  #endregion ISpeckleConverter props

  public ReceiveMode ReceiveMode { get; set; }

  public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();

  public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;

  public void SetPreviousContextObjects(List<ApplicationObject> objects) => throw new NotImplementedException();

  public void SetConverterSettings(object settings)
  {
    Settings = settings as Dictionary<string, string>;
  }

  public void SetContextDocument(object doc)
  {
    Doc = (Document)doc;
    Trans = Doc.TransactionManager.TopTransaction; // set the stream transaction here! make sure it is the top level transaction
  }

  private string ApplicationIdKey = "applicationId";

  public Base ConvertToSpeckle(object @object)
  {
    Base @base = null;
    ApplicationObject reportObj = null;
    DisplayStyle style = null;
    Base extensionDictionary = null;
    List<string> notes = new();

    try
    {
      switch (@object)
      {
        case DBObject obj:

          var appId = obj.ObjectId.ToString(); // TODO: UPDATE THIS WITH STORED APP ID IF IT EXISTS

          //Use the Handle object to update progressReport object.
          //In an AutoCAD session, you can get the Handle of a DBObject from its ObjectId using the ObjectId.Handle or Handle property.
          reportObj = new ApplicationObject(obj.Handle.ToString(), obj.GetType().Name) { applicationId = appId };
          style = DisplayStyleToSpeckle(obj as Entity); // note layer display styles are converted in the layer method
          extensionDictionary = obj.GetObjectExtensionDictionaryAsBase();

          switch (obj)
          {
            case DBPoint o:
              @base = PointToSpeckle(o);
              break;
            case AcadDB.Line o:
              @base = LineToSpeckle(o);
              break;
            case AcadDB.Arc o:
              @base = ArcToSpeckle(o);
              break;
            case AcadDB.Circle o:
              @base = CircleToSpeckle(o);
              break;
            case AcadDB.Ellipse o:
              @base = EllipseToSpeckle(o);
              break;
            case AcadDB.Hatch o:
              @base = HatchToSpeckle(o);
              break;
            case AcadDB.Spline o:
              @base = SplineToSpeckle(o);
              break;
            case AcadDB.Polyline o:
              @base = o.IsOnlyLines ? PolylineToSpeckle(o) : (Base)PolycurveToSpeckle(o);
              break;
            case AcadDB.Polyline3d o:
              @base = PolylineToSpeckle(o);
              break;
            case Polyline2d o:
              @base = PolycurveToSpeckle(o);
              break;
            case Region o:
              @base = RegionToSpeckle(o, out notes);
              break;
            case AcadDB.Surface o:
              @base = SurfaceToSpeckle(o, out notes);
              break;
            case PolyFaceMesh o:
              @base = MeshToSpeckle(o);
              break;
            case ProxyEntity o:
              @base = ProxyEntityToSpeckle(o);
              break;
            case SubDMesh o:
              @base = MeshToSpeckle(o);
              break;
            case Solid3d o:
              if (o.IsNull)
              {
                notes.Add($"Solid was null");
              }
              else
              {
                @base = SolidToSpeckle(o, out notes);
              }

              break;
            case AcadDB.Dimension o:
              @base = DimensionToSpeckle(o);
              break;
            case BlockReference o:
              @base = BlockReferenceToSpeckle(o);
              break;
            case BlockTableRecord o:
              @base = BlockRecordToSpeckle(o);
              break;
            case DBText o:
              @base = TextToSpeckle(o);
              break;
            case MText o:
              @base = TextToSpeckle(o);
              break;
            case LayerTableRecord o:
              @base = LayerToSpeckle(o);
              break;
#if CIVIL
            case CivilDB.Alignment o:
              @base = AlignmentToSpeckle(o);
              break;
            case CivilDB.Corridor o:
              @base = CorridorToSpeckle(o);
              break;
            case CivilDB.FeatureLine o:
              @base = FeaturelineToSpeckle(o);
              break;
            case CivilDB.Structure o:
              @base = StructureToSpeckle(o);
              break;
            case CivilDB.Pipe o:
              @base = PipeToSpeckle(o);
              break;
            case CivilDB.PressurePipe o:
              @base = PipeToSpeckle(o);
              break;
            case CivilDB.Profile o:
              @base = ProfileToSpeckle(o);
              break;
            case CivilDB.TinSurface o:
              @base = SurfaceToSpeckle(o);
              break;
#endif
            default:
#if ADVANCESTEEL
              try
              {
                @base = ConvertASToSpeckle(obj, reportObj, notes);
              }
              catch (Exception e) when (!e.IsFatal())
              {
                //Update report because AS object type
                Report.UpdateReportObject(reportObj);
                throw;
              }
              break;
#else
              throw new ConversionNotSupportedException(
                $"AutocadCivil3D object of type {@object.GetType()} is not supported for conversion."
              );
#endif
          }
          break;
        case Acad.Geometry.Point3d o:
          @base = PointToSpeckle(o);
          break;
        case Acad.Geometry.Vector3d o:
          @base = VectorToSpeckle(o);
          break;
        case Acad.Geometry.Line3d o:
          @base = LineToSpeckle(o);
          break;
        case Acad.Geometry.LineSegment3d o:
          @base = LineToSpeckle(o);
          break;
        case Acad.Geometry.CircularArc3d o:
          @base = ArcToSpeckle(o);
          break;
        case Acad.Geometry.Plane o:
          @base = PlaneToSpeckle(o);
          break;
        case Acad.Geometry.Curve3d o:
          @base = CurveToSpeckle(o) as Base;
          break;
        default:
          throw new ConversionNotSupportedException(
            $"AutocadCivil object of type {@object.GetType()} is not supported for conversion."
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

    if (@base is null)
    {
      return @base;
    }

    if (style != null)
    {
      @base["displayStyle"] = style;
    }

    if (extensionDictionary != null)
    {
      @base["extensionDictionary"] = extensionDictionary;
    }

    if (reportObj != null)
    {
      reportObj.Update(log: notes);
      Report.UpdateReportObject(reportObj);
    }
    return @base;
  }

  private Base ObjectToSpeckleBuiltElement(DBObject o)
  {
    throw new NotImplementedException();
  }

  public List<Base> ConvertToSpeckle(List<object> objects)
  {
    return objects.Select(ConvertToSpeckle).ToList();
  }

  public object ConvertToNative(Base @object)
  {
    // determine if this object has autocad props
    bool isFromAutoCAD = @object[AutocadPropName] != null;
    bool isFromCivil = @object[CivilPropName] != null;
    object acadObj = null;
    var reportObj = Report.ReportObjects.ContainsKey(@object.id)
      ? new ApplicationObject(@object.id, @object.speckle_type)
      : null;
    List<string> notes = new();
    try
    {
      switch (@object)
      {
        case Point o:
          acadObj = PointToNativeDB(o);
          break;

        case Line o:
          acadObj = LineToNativeDB(o);
          break;

        case Arc o:
          acadObj = ArcToNativeDB(o);
          break;

        case Circle o:
          acadObj = CircleToNativeDB(o);
          break;

        case Ellipse o:
          acadObj = EllipseToNativeDB(o);
          break;

        case Spiral o:
          acadObj = PolylineToNativeDB(o.displayValue);
          break;

        case Hatch o:
          acadObj = HatchToNativeDB(o);
          break;

        case Polyline o:
          acadObj = PolylineToNativeDB(o);
          break;

        case Polycurve o:
          bool convertAsSpline = o.segments.Any(s => s is not Line and not Arc);
          acadObj = convertAsSpline || !IsPolycurvePlanar(o) ? PolycurveSplineToNativeDB(o) : PolycurveToNativeDB(o);

          break;

        case Curve o:
          acadObj = CurveToNativeDB(o);
          break;

        case Mesh o:
#if CIVIL
          acadObj = isFromCivil ? CivilSurfaceToNative(o) : MeshToNativeDB(o);
#else
          acadObj = MeshToNativeDB(o);
#endif
          break;

        case Dimension o:
          acadObj = isFromAutoCAD ? AcadDimensionToNative(o) : DimensionToNative(o);
          break;

        case Instance o:
          acadObj = InstanceToNativeDB(o);
          break;

        case BlockDefinition o:
          acadObj = DefinitionToNativeDB(o, out notes);
          break;

        case Text o:
          acadObj = isFromAutoCAD ? AcadTextToNative(o) : TextToNative(o);
          break;

        case Collection o:
          acadObj = CollectionToNative(o);
          break;

#if CIVIL
        case Alignment o:
          acadObj = AlignmentToNative(o);
          break;
#endif

        case ModelCurve o:
          acadObj = CurveToNativeDB(o.baseCurve);
          break;

        case GridLine o:
          acadObj = CurveToNativeDB(o.baseLine);
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
      SpeckleLog.Logger.Error(e, $"{@object.GetType()} unhandled conversion error");
      reportObj?.Update(
        status: ApplicationObject.State.Failed,
        logItem: $"{@object.GetType()} unhandled conversion error: {e.Message}\n{e.StackTrace}"
      );
    }

    switch (acadObj)
    {
      case ApplicationObject o: // some to native methods return an application object (if object is baked to doc during conv)
        acadObj = o.Converted.Count != 0 ? o.Converted : null;
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

    return acadObj;
  }

  public object ConvertToNativeDisplayable(Base @object)
  {
    throw new NotImplementedException();
  }

  public List<object> ConvertToNative(List<Base> objects)
  {
    return objects.Select(ConvertToNative).ToList();
  }

  public bool CanConvertToSpeckle(object @object)
  {
    switch (@object)
    {
      case DBObject o:
        switch (o)
        {
          case DBPoint:
          case AcadDB.Line:
          case AcadDB.Arc:
          case AcadDB.Circle:
          case AcadDB.Dimension:
          case AcadDB.Ellipse:
          case AcadDB.Hatch:
          case AcadDB.Spline:
          case AcadDB.Polyline:
          case Polyline2d:
          case Polyline3d:
          case AcadDB.Surface:
          case PolyFaceMesh:
          case ProxyEntity:
          case AcadDB.Region:
          case SubDMesh:
          case Solid3d:
            return true;

          case BlockReference:
          case BlockTableRecord:
          case DBText:
          case MText:
          case LayerTableRecord:
            return true;

#if CIVIL
          // NOTE: C3D pressure pipes and pressure fittings API under development
          case CivilDB.FeatureLine:
          case CivilDB.Corridor:
          case CivilDB.Structure:
          case CivilDB.Alignment:
          case CivilDB.Pipe:
          case CivilDB.PressurePipe:
          case CivilDB.Profile:
          case CivilDB.TinSurface:
            return true;
#endif

          default:
          {
#if ADVANCESTEEL
              return CanConvertASToSpeckle(o);
#else
            return false;
#endif
          }
        }

      case Acad.Geometry.Point3d:
      case Acad.Geometry.Vector3d:
      case Acad.Geometry.Plane:
      case Acad.Geometry.Line3d:
      case Acad.Geometry.LineSegment3d:
      case Acad.Geometry.CircularArc3d:
      case Acad.Geometry.Curve3d:
        return true;

      default:
        return false;
    }
  }

  public bool CanConvertToNative(Base @object)
  {
    switch (@object)
    {
      case Point:
      case Line:
      case Arc:
      case Circle:
      case Ellipse:
      case Spiral:
      case Hatch:
      case Polyline:
      case Polycurve:
      case Curve:
      case Mesh:

      case Dimension:
      case BlockDefinition:
      case Instance:
      case Text:
      case Collection:

      case Alignment:
      case ModelCurve:
      case GridLine:
        return true;

      default:
        return false;
    }
  }

  public bool CanConvertToNativeDisplayable(Base @object)
  {
    return false;
  }
}
