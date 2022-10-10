using CSiAPIv1;
using Objects.Converter.CSI;
using Objects.Structural.Analysis;
using Objects.Structural.CSI.Geometry;
using Objects.Structural.Results;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BE = Objects.BuiltElements;
using OSEA = Objects.Structural.CSI.Analysis;
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
    public static string CSIAppName = HostApplications.CSIBridge.Name;
    public static string CSISlug = HostApplications.CSIBridge.Slug;
#elif SAFE
      public static string CSIAppName = HostApplications.SAFE.Name;
      public static string CSISlug = HostApplications.SAFE.Slug;
#endif
    public string Description => "Default Speckle Kit for CSI";

    public string Name => nameof(ConverterCSI);

    public string Author => "Speckle";

    public string WebsiteOrEmail => "https://speckle.systems";

    public cSapModel Model { get; private set; }

    public Model SpeckleModel { get; set; }

    public ResultSetAll AnalysisResults { get; set; }

    public ReceiveMode ReceiveMode { get; set; }

    public void SetContextDocument(object doc)
    {
      Model = (cSapModel)doc;
      SpeckleModel = ModelToSpeckle();
      AnalysisResults = ResultsToSpeckle();
    }

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

    public ProgressReport Report { get; private set; } = new ProgressReport();

    public bool CanConvertToNative(Base @object)
    {
      foreach (var type in Enum.GetNames(typeof(ConverterCSI.CSIConverterSupported)))
      {
        if (type == @object.ToString().Split('.').Last())
        {
          return true;
        }
      }
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
      switch (@object)
      {
        case Objects.Organization.Model o:
          return BuiltElementModelToNative(o);
          Report.Log($"Created Model { o.id}");
        //case osg.node o:
        //    return pointtonative(o);
        case OSG.Node o:
          return PointToNative((CSINode)o);
          Report.Log($"Created Node {o.id}");
        case Geometry.Line o:
          return LineToNative(o);
          Report.Log($"Created Line {o.id}");
        case OSG.Element1D o:
          return FrameToNative(o);
          Report.Log($"Created Element1D {o.id}");
        case OSG.Element2D o:
          return AreaToNative(o);
          Report.Log($"Created Element2D {o.id}");
        case Model o:
          return ModelToNative(o);
          Report.Log($"Created Model {o.id}");
        default:
          Report.Log($"Skipped not supported type: {@object.GetType()} {@object.id}");
          throw new NotSupportedException();
      }
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
          returnObject = AnalysisResults;
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
      return returnObject;
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public IEnumerable<string> GetServicedApplications() => new string[] { CSIAppName };


    public void SetContextObjects(List<ApplicationObject> objects)
    {
      throw new NotImplementedException();
    }

    public void SetPreviousContextObjects(List<ApplicationObject> objects)
    {
      throw new NotImplementedException();
    }

    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }
  }
}