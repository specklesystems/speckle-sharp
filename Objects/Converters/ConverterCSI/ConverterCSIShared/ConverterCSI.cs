using CSiAPIv1;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.CSI.Properties;
using Objects.Structural.Geometry;
using Objects.Structural.Loading;
using Objects.Structural.Properties;
using Objects.Structural.Results;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using OSG = Objects.Structural.Geometry;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI : ISpeckleConverter
  {
#if ETABS
    public static string CSIAppName = HostApplications.ETABS.Name;
    public static string CSISlug = HostApplications.ETABS.Slug;
#elif SAP2000
    public static string CSIAppName = HostApplications.SAP2000.Name;
    public static string CSISlug = HostApplications.SAP2000.Slug;
#elif CSIBRIDGE
    public static string CSIAppName = HostApplications.CSiBridge.Name;
    public static string CSISlug = HostApplications.CSiBridge.Slug;
#elif SAFE
    public static string CSIAppName = HostApplications.SAFE.Name;
    public static string CSISlug = HostApplications.SAFE.Slug;
#endif
    public string Description => "Default Speckle Kit for CSI";

    public string Name => nameof(ConverterCSI);

    public string Author => "Speckle";

    public string WebsiteOrEmail => "https://speckle.systems";

    public cSapModel Model { get; private set; }
    public string ProgramVersion { get; private set; }

    public Model SpeckleModel { get; set; }

    public ReceiveMode ReceiveMode { get; set; }
    /// <summary>
    /// <para>To know which objects are already in the model. These are *mostly* elements that are in the model before the receive operation starts, but certain names will be added for objects that may be referenced by other elements such as load patterns and load cases.</para>
    /// <para> The keys are typically GUIDS and the values are exclusively names. It is easier to retrieve names, and names are typically used by the api, however GUIDS are more stable and can't be changed in the user interface. Some items (again load patterns and load combinations) don't have GUIDs so those just store the name value twice. </para>
    /// </summary>
    public Dictionary<string, string> ExistingObjectGuids { get; set; }

    /// <summary>
    /// <para>To know which other objects are being converted, in order to sort relationships between them.
    /// For example, elements that have children use this to determine whether they should send their children out or not.</para>
    /// </summary>
    public List<ApplicationObject> ContextObjects { get; set; } = new List<ApplicationObject>();

    /// <summary>
    /// <para>To keep track of previously received objects from a given stream in here. If possible, conversions routines
    /// will edit an existing object, otherwise they will delete the old one and create the new one.</para>
    /// </summary>
    public List<ApplicationObject> PreviousContextObjects { get; set; } = new List<ApplicationObject>();
    public Dictionary<string, string> Settings { get; private set; } = new Dictionary<string, string>();
    public void SetContextObjects(List<ApplicationObject> objects) => ContextObjects = objects;
    public void SetPreviousContextObjects(List<ApplicationObject> objects) => PreviousContextObjects = objects;
    public void SetContextDocument(object doc)
    {
      Model = (cSapModel)doc;
      double version = 0;
      string versionString = null;
      Model.GetVersion(ref versionString, ref version);
      ProgramVersion = versionString;

      if (!Settings.ContainsKey("operation"))
        throw new Exception("operation setting was not set before calling converter.SetContextDocument");

      if (Settings["operation"] == "receive")
      { 
        ExistingObjectGuids = GetAllGuids(Model);
        // TODO: make sure we are setting the load patterns before we import load combinations
      }
      else if (Settings["operation"] == "send")
      {
        SpeckleModel = ModelToSpeckle();
      }
      else
        throw new Exception("operation setting was not set to \"send\" or \"receive\"");
    }
    public void SetConverterSettings(object settings)
    {
      Settings = settings as Dictionary<string, string>;
    }

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();
    public ProgressReport Report { get; private set; } = new ProgressReport();

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case CSIDiaphragm _:
        case CSIStories _:
        case Element1D _:
        case Element2D _:
        case Load _:
        //case Geometry.Line line:
        case Node _:
        //case Model o:
        //case Property property:

        // for the moment we need to have this in here so the flatten traversal skips over this object
        // otherwise it would add result.element to the list twice and the stored objects dictionary would throw
        case Result _:
        case BuiltElements.Beam _:
        case BuiltElements.Brace _:
        case BuiltElements.Column _:
          return true;
      };
      return false;
    }

    public bool CanConvertToSpeckle(object @object)
    {
      if (@object == null)
        return false;
      foreach (var type in Enum.GetNames(typeof(ConverterCSI.CSIAPIUsableTypes)))
      {
        if (type == @object.ToString())
        {
          return true;
        }
      }
      return false;
    }

    public object ConvertToNative(Base @object)
    {
      var appObj = new ApplicationObject(@object.id, @object.speckle_type) { applicationId = @object.applicationId };
      List<string> notes = new List<string>();

      switch (@object)
      {
        case CSIAreaSpring o:
          AreaSpringPropertyToNative(o, ref appObj);
          break;
        case CSIDiaphragm o:
          DiaphragmToNative(o, ref appObj);
          break;
        case CSILinearSpring o:
          LinearSpringPropertyToNative(o, ref appObj);
          break;
        case CSILinkProperty o:
          LinkPropertyToNative(o, ref appObj);
          break;
        case CSIProperty2D o:
          Property2DToNative(o, ref appObj);
          break;
        case CSISpringProperty o:
          SpringPropertyToNative(o, ref appObj);
          break;
        case CSIStories o:
          StoriesToNative(o, ref appObj);
          break;
        //case CSIWindLoadingFace o:
        //  LoadFaceToNative(o, ref appObj);
        //  break;
        //case CSITendonProperty o:
        case OSG.Element1D o:
          FrameToNative(o, ref appObj);
          break;
        case OSG.Element2D o:
          AreaToNative(o, ref appObj);
          break;
        case LoadBeam o:
          LoadFrameToNative(o, ref appObj);
          break;
        case LoadFace o:
          LoadFaceToNative(o, ref appObj);
          break;
        //case osg.node o:
        //    return pointtonative(o);
        case Geometry.Line o:
          LineToNative(o, ref appObj); // do we really want to assume any line is a frame object?
          break;
        case OSG.Node o:
          PointToNative(o, ref appObj);
          break;
        case Property1D o:
          Property1DToNative(o, ref appObj);
          break;
        #region BuiltElements
        case BuiltElements.Beam o:
          CurveBasedElementToNative(o, o.baseLine, ref appObj);
          break;
        case BuiltElements.Brace o:
          CurveBasedElementToNative(o, o.baseLine, ref appObj);
          break;
        case BuiltElements.Column o:
          CurveBasedElementToNative(o, o.baseLine, ref appObj);
          break;
        #endregion
        default:
          appObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Skipped not supported type: {@object.GetType()}");
          break;
      }

      // log 
      var reportObj = Report.ReportObjects.ContainsKey(@object.id) ? Report.ReportObjects[@object.id] : null;
      if (reportObj != null && notes.Count > 0)
        reportObj.Update(log: notes);

      return appObj;
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    public Base ConvertToSpeckle(object @object)
    {
      (string type, string name) = ((string, string))@object;
      Base returnObject = null;
      switch (type)
      {
        case "Point":
          returnObject = PointToSpeckle(name);
          Report.Log($"Created Node");
          break;
        case "Frame":
          returnObject = FrameToSpeckle(name);
          Report.Log($"Created Frame");
          break;
        case "Model":
          returnObject = SpeckleModel;
          break;
        case "AnalysisResults":
          returnObject = ResultsToSpeckle();
          break;
        case "Stories":
          returnObject = StoriesToSpeckle();
          break;
        case "Area":
          returnObject = AreaToSpeckle(name);
          Report.Log($"Created Area");
          break;
        case "Wall":
          returnObject = WallToSpeckle(name);
          Report.Log($"Created Wall");
          break;
        case "Floor":
          returnObject = FloorToSpeckle(name);
          Report.Log($"Created Floor");
          break;
        case "Column":
          returnObject = ColumnToSpeckle(name);
          Report.Log($"Created Column");
          break;
        case "Beam":
          returnObject = BeamToSpeckle(name);
          Report.Log($"Created Beam");
          break;
        case "Brace":
          returnObject = BraceToSpeckle(name);
          Report.Log($"Created Brace");
          break;
        case "Link":
          returnObject = LinkToSpeckle(name);
          Report.Log($"Created Link");
          break;
        case "ElementsCount":
          returnObject = ModelElementsCountToSpeckle();
          break;
        case "Spandrel":
          returnObject = SpandrelToSpeckle(name);
          Report.Log($"Created Spandrel");
          break;
        case "Pier":
          returnObject = PierToSpeckle(name);
          Report.Log($"Created Pier");
          break;
        case "Grids":
          returnObject = gridLinesToSpeckle(name);
          Report.Log($"Created Grids");
          break;
        case "Tendon":
          returnObject = CSITendonToSpeckle(name);
          Report.Log($"Created Tendons");
          break;
        //case "Diaphragm":
        //  returnObject = diaphragmToSpeckle(name);
        //  Report.Log($"Created Diaphragm");
        case "Links":
          returnObject = LinkToSpeckle(name);
          break;
        //case "LoadCase":
        //    returnObject = LoadCaseToSpeckle(name);
        //    break;
        case "BeamLoading":
          returnObject = LoadFrameToSpeckle(name, GetBeamNames(Model).Count());
          Report.Log($"Created Loading Beam");
          break;
        case "ColumnLoading":
          returnObject = LoadFrameToSpeckle(name, GetColumnNames(Model).Count());
          Report.Log($"Created Loading Column");
          break;
        case "BraceLoading":
          returnObject = LoadFrameToSpeckle(name, GetBraceNames(Model).Count());
          Report.Log($"Created Loading Brace");
          break;
        case "FrameLoading":
          returnObject = LoadFrameToSpeckle(name, GetAllFrameNames(Model).Count());
          Report.Log($"Created Loading Frame");
          break;
        case "FloorLoading":
          returnObject = LoadFaceToSpeckle(name, GetAllFloorNames(Model).Count());
          Report.Log($"Created Loading Floor");
          break;
        case "WallLoading":
          returnObject = LoadFaceToSpeckle(name, GetAllWallNames(Model).Count());
          Report.Log($"Created Loading Wall");
          break;
        case "AreaLoading":
          returnObject = LoadFaceToSpeckle(name, GetAllAreaNames(Model).Count());
          Report.Log($"Created Loading Area");
          break;
        case "NodeLoading":
          returnObject = LoadNodeToSpeckle(name, GetAllPointNames(Model).Count());
          Report.Log($"Created Loading Node");
          break;
        case "LoadPattern":
          returnObject = LoadPatternToSpeckle(name);
          Report.Log($"Created Loading Pattern");
          break;
          //case "ColumnResults":
          //    returnObject = FrameResultSet1dToSpeckle(name);
          //    break;
          //case "BeamResults":
          //    returnObject = FrameResultSet1dToSpeckle(name);
          //    break;
          //case "BraceResults":
          //    returnObject = FrameResultSet1dToSpeckle(name);
          //    break;
          //case "PierResults":
          //    returnObject = PierResultSet1dToSpeckle(name);
          //    break;
          //case "SpandrelResults":
          //    returnObject = SpandrelResultSet1dToSpeckle(name);
          //    break;
          //case "GridSys":
          //    returnObject = GridSysToSpeckle(name);
          //    break;
          //case "Combo":
          //    returnObject = ComboToSpeckle(name);
          //    break;
          //case "DesignSteel":
          //    returnObject = DesignSteelToSpeckle(name);
          //    break;
          //case "DeisgnConcrete":
          //    returnObject = DesignConcreteToSpeckle(name);
          //    break;
          //case "Story":
          //    returnObject = StoryToSpeckle(name);
          //    break;
          //case "Diaphragm":
          //    returnObject = DiaphragmToSpeckle(name);
          //    break;
          //case "PierLabel":
          //    returnObject = PierLabelToSpeckle(name);
          //    break;
          //case "PropAreaSpring":
          //    returnObject = PropAreaSpringToSpeckle(name);
          //    break;
          //case "PropLineSpring":
          //    returnObject = PropLineSpringToSpeckle(name);
          //    break;
          //case "PropPointSpring":
          //    returnObject = PropPointSpringToSpeckle(name);
          //    break;
          //case "SpandrelLabel":
          //    returnObject = SpandrelLabelToSpeckle(name);
          //    break;
          //case "PropTendon":
          //    returnObject = PropTendonToSpeckle(name);
          //    break;
          //case "PropLink":
          //    returnObject = PropLinkToSpeckle(name);
          //    break;
          //default:
          //    ConversionErrors.Add(new SpeckleException($"Skipping not supported type: {type}"));
          //    returnObject = null;
          //    break;
      }

      // send the object out with the same appId that it came in with for updating purposes
      if (returnObject != null)
        returnObject.applicationId = GetOriginalApplicationId(returnObject.applicationId);

      return returnObject;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public IEnumerable<string> GetServicedApplications() => new string[] { CSIAppName };
  }
}
