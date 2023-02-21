using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit
{
  public static class Categories
  {
    public static readonly List<BuiltInCategory> columnCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns };
    public static readonly List<BuiltInCategory> beamCategories = new List<BuiltInCategory> { BuiltInCategory.OST_StructuralFraming };
    public static readonly List<BuiltInCategory> ductCategories = new List<BuiltInCategory> { BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_FlexDuctCurves };
    public static readonly List<BuiltInCategory> pipeCategories = new List<BuiltInCategory> { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_FlexPipeCurves };
    public static readonly List<BuiltInCategory> wallCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Walls };
    public static readonly List<BuiltInCategory> floorCategories = new List<BuiltInCategory> { BuiltInCategory.OST_Floors };
    public static readonly List<BuiltInCategory> curtainWallSubElements = new List<BuiltInCategory> { BuiltInCategory.OST_CurtainWallMullions, BuiltInCategory.OST_CurtainWallPanels };

    public static bool Contains(this IEnumerable<BuiltInCategory> categories, Category category)
    {
      try
      {
        return categories.Select(x => (int)x).Contains(category.Id.IntegerValue);
      }
      catch
      {
        return false;
      }
    }

    public static bool IsElementSupported(this Element e)
    {
      if (e.Category == null) return false;
      if (e.ViewSpecific) return false;

      if (SupportedBuiltInCategories.Contains((BuiltInCategory)e.Category.Id.IntegerValue))
        return true;
      return false;
    }

    //list of currently supported Categories (for sending only)
    //exact copy of the one in the Speckle.ConnectorRevit.ConnectorRevitUtils
    //until issue https://github.com/specklesystems/speckle-sharp/issues/392 is resolved
    private static List<BuiltInCategory> SupportedBuiltInCategories = new List<BuiltInCategory>{

      BuiltInCategory.OST_Areas,
      BuiltInCategory.OST_CableTray,
      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_Columns,
      BuiltInCategory.OST_CommunicationDevices,
      BuiltInCategory.OST_Conduit,
      BuiltInCategory.OST_CurtaSystem,
      BuiltInCategory.OST_DataDevices,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_DuctSystem,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctInsulations,
      BuiltInCategory.OST_ElectricalCircuit,
      BuiltInCategory.OST_ElectricalEquipment,
      BuiltInCategory.OST_ElectricalFixtures,
      BuiltInCategory.OST_Fascia,
      BuiltInCategory.OST_FireAlarmDevices,
      BuiltInCategory.OST_FlexDuctCurves,
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_Grids,
      BuiltInCategory.OST_Gutter,
      //BuiltInCategory.OST_HVAC_Zones,
      BuiltInCategory.OST_IOSModelGroups,
      BuiltInCategory.OST_LightingDevices,
      BuiltInCategory.OST_LightingFixtures,
      BuiltInCategory.OST_Lines,
      BuiltInCategory.OST_Mass,
      BuiltInCategory.OST_MassFloor,
      BuiltInCategory.OST_Materials,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_MEPSpaces,
      BuiltInCategory.OST_Parking,
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_PipingSystem,
      BuiltInCategory.OST_PointClouds,
      BuiltInCategory.OST_PointLoads,
      BuiltInCategory.OST_StairsRailing,
      BuiltInCategory.OST_RailingSupport,
      BuiltInCategory.OST_RailingTermination,
      BuiltInCategory.OST_Rebar,
      BuiltInCategory.OST_Roads,
      BuiltInCategory.OST_RoofSoffit,
      BuiltInCategory.OST_Roofs,
      BuiltInCategory.OST_Rooms,
      BuiltInCategory.OST_SecurityDevices,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_Site,
      BuiltInCategory.OST_EdgeSlab,
      BuiltInCategory.OST_Stairs,
      BuiltInCategory.OST_AreaRein,
      BuiltInCategory.OST_StructuralFramingSystem,
      BuiltInCategory.OST_StructuralColumns,
      BuiltInCategory.OST_StructConnections,
      BuiltInCategory.OST_FabricAreas,
      BuiltInCategory.OST_FabricReinforcement,
      BuiltInCategory.OST_StructuralFoundation,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_PathRein,
      BuiltInCategory.OST_StructuralStiffener,
      BuiltInCategory.OST_StructuralTruss,
      BuiltInCategory.OST_SwitchSystem,
      BuiltInCategory.OST_TelephoneDevices,
      BuiltInCategory.OST_Topography,
      BuiltInCategory.OST_Cornices,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_Wire,
      BuiltInCategory.OST_Casework,
      BuiltInCategory.OST_CurtainWallPanels,
      BuiltInCategory.OST_CurtainWallMullions,
      BuiltInCategory.OST_Entourage,
      BuiltInCategory.OST_Furniture,
      BuiltInCategory.OST_FurnitureSystems,
      BuiltInCategory.OST_Planting,
      BuiltInCategory.OST_PlumbingFixtures,
      BuiltInCategory.OST_Ramps,
      BuiltInCategory.OST_SpecialityEquipment,
      BuiltInCategory.OST_Rebar,
#if REVIT2020 || REVIT2021
#else
      BuiltInCategory.OST_AudioVisualDevices,
      BuiltInCategory.OST_FireProtection,
      BuiltInCategory.OST_FoodServiceEquipment,
      BuiltInCategory.OST_Hardscape,
      BuiltInCategory.OST_MedicalEquipment,
      BuiltInCategory.OST_Signage,
      BuiltInCategory.OST_TemporaryStructure,
      BuiltInCategory.OST_VerticalCirculation,
#endif
#if REVIT2020 || REVIT2021 || REVIT2022
#else
       BuiltInCategory.OST_MechanicalControlDevices,
#endif

  };

    public static RevitCategory GetSchemaBuilderCategoryFromBuiltIn(string builtInCategory)
    {
      return (RevitCategory)BuiltInCategoryNames.IndexOf(builtInCategory);
    }

    public static string GetBuiltInFromSchemaBuilderCategory(RevitCategory c)
    {
      return BuiltInCategoryNames[(int)c];
    }

    //This list is used to retrieve BuiltIn names of the categories used by Schema builder
    //List adapted from the on used by from Rhino Inside
    //NOTE: if edited the list in Objects.BuiltElements.Revit.Enums should be updated too
    internal static List<string> BuiltInCategoryNames = new List<string>
    {
      "OST_AbutmentFoundations",
      "OST_AbutmentPiles",
      "OST_AbutmentWalls",
      "OST_BridgeAbutments",
      "OST_DuctTerminal",
      "OST_Alignments",
      "OST_StructConnectionAnchors",
      "OST_ApproachSlabs",
      "OST_BridgeArches",
      "OST_AudioVisualDevices",
      "OST_StairsRailingBaluster",
      "OST_BridgeBearings",
      "OST_StructConnectionBolts",
      "OST_BridgeCables",
      "OST_BridgeDecks",
      "OST_BridgeFraming",
      "OST_CableTrayFitting",
      "OST_CableTrayRun",
      "OST_CableTray",
      "OST_Casework",
      "OST_Ceilings",
      "OST_Columns",
      "OST_CommunicationDevices",
      "OST_ConduitFitting",
      "OST_Conduit",
      "OST_Coordination_Model",
      "OST_BridgeFramingCrossBracing",
      "OST_CurtainWallPanels",
      "OST_CurtaSystem",
      "OST_CurtainWallMullions",
      "OST_DataDevices",
      "OST_BridgeFramingDiaphragms",
      "OST_Doors",
      "OST_DuctAccessory",
      "OST_DuctFitting",
      "OST_PlaceHolderDucts",
      "OST_DuctSystem",
      "OST_DuctCurves",
      "OST_ElectricalEquipment",
      "OST_ElectricalFixtures",
      "OST_Entourage",
      "OST_ExpansionJoints",
      "OST_FireAlarmDevices",
      "OST_FireProtection",
      "OST_Floors",
      "OST_FoodServiceEquipment",
      "OST_Furniture",
      "OST_FurnitureSystems",
      "OST_GenericAnnotation",
      "OST_GenericModel",
      "OST_BridgeGirders",
      "OST_Hardscape",
      "OST_LightingDevices",
      "OST_LightingFixtures",
      "OST_Lines",
      "OST_Mass",
      "OST_MechanicalEquipment",
      "OST_MedicalEquipment",
      "OST_NurseCallDevices",
      "OST_Parking",
      "OST_Parts",
      "OST_PierCaps",
      "OST_PierColumns",
      "OST_BridgeFoundations",
      "OST_PierPiles",
      "OST_BridgeTowers",
      "OST_PierWalls",
      "OST_BridgePiers",
      "OST_PipeAccessory",
      "OST_PipeFitting",
      "OST_PlaceHolderPipes",
      "OST_PipeSegments",
      "OST_PipeCurves",
      "OST_PipingSystem",
      "OST_Planting",
      "OST_StructConnectionPlates",
      "OST_PlumbingFixtures",
      "OST_StructConnectionProfiles",
      "OST_StairsRailing",
      "OST_Ramps",
      "OST_Roads",
      "OST_Roofs",
      "OST_SecurityDevices",
      "OST_StructConnectionShearStuds",
      "OST_Signage",
      "OST_Site",
      "OST_SpecialityEquipment",
      "OST_Sprinklers",
      "OST_Stairs",
      "OST_StructuralFramingSystem",
      "OST_StructuralColumns",
      "OST_StructConnections",
      "OST_FabricAreas",
      "OST_StructuralFoundation",
      "OST_StructuralFraming",
      "OST_Rebar",
      "OST_Coupler",
      "OST_StructuralStiffener",
      "OST_StructuralTendons",
      "OST_StructuralTruss",
      "OST_TemporaryStructure",
      "OST_Topography",
      "OST_BridgeFramingTrusses",
      "OST_VerticalCirculation",
      "OST_VibrationDampers",
      "OST_VibrationIsolators",
      "OST_VibrationManagement",
      "OST_Walls",
      "OST_StructConnectionWelds",
      "OST_Windows"
    };


  }
}
