using System;
using System.Collections.Generic;
using Speckle.Core.Logging;
using System.Linq;
using CSiAPIv1;

namespace Speckle.ConnectorCSI.Util
{
  public static class ConnectorCSIUtils
  {
    //#if ETABS
    //    public static string CSIAppName = VersionedHostApplications.ETABS;
    //    public static string CSISlug = HostApplications.ETABS.Slug;
    //#elif SAP2000
    //    public static string CSIAppName = VersionedHostApplications.SAP2000;
    //        public static string CSISlug = HostApplications.SAP2000.Slug;
    //#elif CSIBridge
    //    public static string CSIAppName = VersionedHostApplications.CSIBridge;
    //        public static string CSISlug = HostApplications.CSIBridge.Slug;
    //#elif SAFE
    //  public static string CSIAppName = VersionedHostApplications.SAFE;
    //  public static string CSISlug = HostApplications.SAFE.Slug;
    //#else
    //    public static string CSIAppName = VersionedHostApplications.CSI;
    //    public static string CSISlug = HostApplications.CSI.Slug;
    //#endif

    public static Dictionary<string, (string, string)> ObjectIDsTypesAndNames { get; set; }

    public static List<SpeckleException> ConversionErrors { get; set; }

    // warning: this delimter string needs to be the same as the delimter string in "converterCSIUtils"
    public static string delimiter = "::";

    public static void GetObjectIDsTypesAndNames(cSapModel model)
    {
      ObjectIDsTypesAndNames = new Dictionary<string, (string, string)>();
      foreach (var objectType in Enum.GetNames(typeof(CSIAPIUsableTypes)))
      {
        var names = new List<string>();
        try
        {
          names = GetAllNamesOfObjectType(model, objectType);
        }
        catch { }
        if (names.Count > 0)
        {
          foreach (string name in names)
          {
            ObjectIDsTypesAndNames.Add(string.Concat(objectType, ": ", name), (objectType, name));
          }
        }
      }
    }

    public static bool IsTypeCSIAPIUsable(string type)
    {
      return Enum.GetNames(typeof(CSIAPIUsableTypes)).Contains(type);
    }

    public static List<string> GetAllNamesOfObjectType(cSapModel model, string objectType)
    {
      switch (objectType)
      {
        case "Point":
          return GetAllPointNames(model);
        case "Frame":
          return GetAllFrameNames(model);
        case "Beam":
          return GetBeamNames(model);
        case "Column":
          return GetColumnNames(model);
        case "Brace":
          return GetBraceNames(model);
        case "Area":
          return GetAllAreaNames(model);
        case "Floor":
          return GetAllFloorNames(model);
        case "Wall":
          return GetAllWallNames(model);
        case "Links":
          return GetAllLinkNames(model);
        case "Spandrel":
          return GetAllSpandrelLabelNames(model);
        case "Tendon":
          return GetAllTendonNames(model);
        case "Pier":
          return GetAllPierLabelNames(model);
        case "Grids":
          return GetAllGridNames(model);
        case "LoadPattern":
          return GetAllLoadPatternNames(model);
        case "BeamLoading":
          return GetBeamNames(model);
        case "ColumnLoading":
          return GetColumnNames(model);
        case "BraceLoading":
          return GetBraceNames(model);
        case "FrameLoading":
          return GetAllFrameNames(model);
        case "FloorLoading":
          return GetAllFloorNames(model);
        case "WallLoading":
          return GetAllWallNames(model);
        case "AreaLoading":
          return GetAllAreaNames(model);
        case "NodeLoading":
          return GetAllPointNames(model);
        case "Model":
          var names = new string[] { };
          names.Append(model.GetModelFilename());
          return names.ToList();
        case "ColumnResults":
          return GetColumnNames(model);
        case "BeamResults":
          return GetBeamNames(model);
        case "BraceResults":
          return GetBraceNames(model);
        case "PierResults":
          return GetAllPierLabelNames(model);
        case "SpandrelResults":
          return GetAllSpandrelLabelNames(model);
        case "AnalysisResults":
          return GetAllElementNames(model);
        default:
          return null;
      }
    }
    #region Get List Names
    public static List<string> GetAllPointNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PointObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }

    }
    public static List<string> GetAllFrameNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.FrameObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }

    public static List<string> GetColumnNames(cSapModel model)
    {
      var frameNames = GetAllFrameNames(model);

      List<string> columnNames = new List<string>();

      string frameLabel = "";
      string frameStory = "";

      foreach (var frameName in frameNames)
      {
        model.FrameObj.GetLabelFromName(frameName, ref frameLabel, ref frameStory);

        if (frameLabel.ToLower().StartsWith("c"))
        {
          columnNames.Add(frameName);
        }
      }

      return columnNames;
    }

    public static List<string> GetBeamNames(cSapModel model)
    {
      var frameNames = GetAllFrameNames(model);

      List<string> beamNames = new List<string>();

      string frameLabel = "";
      string frameStory = "";

      foreach (var frameName in frameNames)
      {
        model.FrameObj.GetLabelFromName(frameName, ref frameLabel, ref frameStory);

        if (frameLabel.ToLower().StartsWith("b"))
        {
          beamNames.Add(frameName);
        }
      }

      return beamNames;
    }

    public static List<string> GetBraceNames(cSapModel model)
    {
      var frameNames = GetAllFrameNames(model);

      List<string> braceNames = new List<string>();

      string frameLabel = "";
      string frameStory = "";

      foreach (var frameName in frameNames)
      {
        model.FrameObj.GetLabelFromName(frameName, ref frameLabel, ref frameStory);

        if (frameLabel.ToLower().StartsWith("d"))
        {
          braceNames.Add(frameName);
        }
      }

      return braceNames;
    }

    public static List<string> GetAllElementNames(cSapModel model)
    {
      var elementNames = new List<string>();

      elementNames.AddRange(GetColumnNames(model));
      elementNames.AddRange(GetBeamNames(model));
      elementNames.AddRange(GetBraceNames(model));
      elementNames.AddRange(GetAllPierLabelNames(model));
      elementNames.AddRange(GetAllSpandrelLabelNames(model));

      return elementNames;
    }

    public static List<string> GetAllTendonNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.TendonObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllAreaNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.AreaObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }

    public static List<string> GetAllWallNames(cSapModel model)
    {
      var WallNames = GetAllAreaNames(model);

      List<string> WallName = new List<string>();

      string wallLabel = "";
      string wallStory = "";

      foreach (var wallName in WallNames)
      {
        model.AreaObj.GetLabelFromName(wallName, ref wallLabel, ref wallStory);

        if (wallLabel.ToLower().StartsWith("w"))
        {
          WallName.Add(wallName);
        }
      }

      return WallName;
    }
    public static List<string> GetAllFloorNames(cSapModel model)
    {
      var FloorNames = GetAllAreaNames(model);

      List<string> FloorName = new List<string>();

      string FloorLabel = "";
      string FloorStory = "";

      foreach (var floorName in FloorNames)
      {
        model.AreaObj.GetLabelFromName(floorName, ref FloorLabel, ref FloorStory);

        if (FloorLabel.ToLower().StartsWith("f"))
        {
          FloorName.Add(floorName);
        }
      }

      return FloorName;
    }
    public static List<string> GetAllLinkNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.LinkObj.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropMaterialNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropMaterial.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropRebarNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropRebar.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropFrameNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropFrame.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllLoadCaseNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.LoadCases.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllGroupNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.GroupDef.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllGridNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.GridSys.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllComboNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.RespCombo.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllConstraintNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.ConstraintDef.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllLoadPatternNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.LoadPatterns.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllSteelDesignNames(cSapModel model)
    {
      var name = "";
      try
      {
        model.DesignSteel.GetCode(ref name);
        return new List<string>() { name };
      }
      catch { return null; }
    }
    public static List<string> GetAllConcreteDesignNames(cSapModel model)
    {
      var name = "";
      try
      {
        model.DesignConcrete.GetCode(ref name);
        return new List<string>() { name };
      }
      catch { return null; }
    }
    public static List<string> GetAllStoryNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.Story.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllDiaphragmNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.Diaphragm.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllLineNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.LineElm.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPierLabelNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PierLabel.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropAreaSpringNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropAreaSpring.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropLineSpringNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropLineSpring.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropPointSpringNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropPointSpring.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllSpandrelLabelNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      var isMultiStory = new bool[] { };
      try
      {
        model.SpandrelLabel.GetNameList(ref num, ref names, ref isMultiStory);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllTowerNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.Tower.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropTendonNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropTendon.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }
    public static List<string> GetAllPropLinkNames(cSapModel model)
    {
      int num = 0;
      var names = new string[] { };
      try
      {
        model.PropLink.GetNameList(ref num, ref names);
        return names.ToList();
      }
      catch { return null; }
    }

    #endregion

    public static List<(string, string)> SelectedObjects(cSapModel model)
    {
      int num = 0;
      var types = new int[] { };
      var names = new string[] { };
      model.SelectObj.GetSelected(ref num, ref types, ref names);
      var typesAndNames = new List<(string, string)>();
      if (num < 1)
      {
        return null;
      }
      for (int i = 0; i < num; i++)
      {
        switch (types[i])
        {
          case 1:
            typesAndNames.Add(("Point", names[i]));
            break;
          case 2:
            typesAndNames.Add(("Frame", names[i]));
            break;
          case 3:
            typesAndNames.Add(("Cable", names[i]));
            break;
          case 4:
            typesAndNames.Add(("Tendon", names[i]));
            break;
          case 5:
            typesAndNames.Add(("Area", names[i]));
            break;
          case 6:
            typesAndNames.Add(("Solid", names[i]));
            break;
          case 7:
            typesAndNames.Add(("Link", names[i]));
            break;
          default:
            break;
        }
      }
      return typesAndNames;
    }

    /// <summary>
    /// Removes all inherited classes from speckle type string (copied from Revit converter)
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string SimplifySpeckleType(string type)
    {
      return type.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
    }

    public enum CSIAPIUsableTypes
    {
      Point, // cPointObj
      Frame, // cFrameObj
      Beam,
      Column,
      Brace,
      Area,
      Wall,
      Spandrel,
      Pier,
      Floor,
      Grids,
      Links,
      Tendon,
      LoadPattern,
      Model,
      //Diaphragm,
      BeamLoading,
      ColumnLoading,
      BraceLoading,
      FrameLoading,
      FloorLoading,
      AreaLoading,
      WallLoading,
      NodeLoading,


      //ColumnResults,
      //BeamResults,
      //BraceResults,
      //PierResults,
      //SpandrelResults,
      //AnalysisResults
    }

    /// <summary>
    /// same as ObjectType in CSI cSelect.GetSelected API function
    /// </summary>
    public enum CSIViewSelectableTypes
    {
      Point = 1,
      Frame = 2,
      Area = 4
    }
  }

}