using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace RevitSharedResources.Helpers;

/// <summary>
/// Contains all shared logic related to Categories between Converter and Connector
/// </summary>
public static class Categories
{
  public static readonly List<BuiltInCategory> columnCategories =
    new() { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns };

  public static readonly List<BuiltInCategory> beamCategories = new() { BuiltInCategory.OST_StructuralFraming };

  public static readonly List<BuiltInCategory> ductCategories =
    new() { BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_FlexDuctCurves };

  public static readonly List<BuiltInCategory> pipeCategories =
    new() { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_FlexPipeCurves };

  public static readonly List<BuiltInCategory> wallCategories = new() { BuiltInCategory.OST_Walls };
  public static readonly List<BuiltInCategory> floorCategories = new() { BuiltInCategory.OST_Floors };

  public static readonly List<BuiltInCategory> curtainWallSubElements =
    new() { BuiltInCategory.OST_CurtainWallMullions, BuiltInCategory.OST_CurtainWallPanels };

  /// <summary>
  /// List of currently supported Categories (for sending only)
  /// The contained items will vary depending on the target Revit version
  /// </summary>
  public static IReadOnlyList<BuiltInCategory> SupportedBuiltInCategories = new List<BuiltInCategory>
  {
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
}
