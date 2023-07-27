using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;

namespace RevitSharedResources.Helpers;

/// <summary>
/// Contains all shared logic related to Categories between Converter and Connector
/// </summary>
public static class Categories
{
  public static IReadOnlyList<BuiltInCategory> SupportedBuiltInCategories = new List<BuiltInCategory>
  {
    BuiltInCategory.OST_Areas,
    BuiltInCategory.OST_CableTrayFitting,
    BuiltInCategory.OST_CableTray,
    BuiltInCategory.OST_Ceilings,
    BuiltInCategory.OST_Columns,
    BuiltInCategory.OST_CommunicationDevices,
    BuiltInCategory.OST_Conduit,
    BuiltInCategory.OST_ConduitFitting,
    BuiltInCategory.OST_CurtaSystem,
    BuiltInCategory.OST_DataDevices,
    BuiltInCategory.OST_Doors,
    BuiltInCategory.OST_DuctAccessory,
    BuiltInCategory.OST_DuctSystem,
    BuiltInCategory.OST_DuctCurves,
    BuiltInCategory.OST_DuctFitting,
    BuiltInCategory.OST_DuctInsulations,
    BuiltInCategory.OST_DuctTerminal,
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
    BuiltInCategory.OST_PipeAccessory,
    BuiltInCategory.OST_PipeCurves,
    BuiltInCategory.OST_PipeFitting,
    BuiltInCategory.OST_PipingSystem,
    BuiltInCategory.OST_PipeInsulations,
    BuiltInCategory.OST_PointClouds,
    BuiltInCategory.OST_PointLoads,
    BuiltInCategory.OST_StairsRailing,
    BuiltInCategory.OST_RailingSupport,
    BuiltInCategory.OST_RailingTermination,
    BuiltInCategory.OST_Railings,
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

  public static Dictionary<string, RevitCategoryInfo> All { get; } 

  static Categories()
  {
    All = new(StringComparer.OrdinalIgnoreCase)
    {
      { nameof(CableTray), CableTray },
      { nameof(Ceiling), Ceiling },
      { nameof(Column), Column },
      { nameof(Conduit), Conduit },
      { nameof(Door), Door },
      { nameof(Duct), Duct },
      { nameof(FamilyInstance), FamilyInstance },
      { nameof(Floor), Floor },
      { nameof(Furniture), Furniture },
      { nameof(Pipe), Pipe },
      { nameof(PlumbingFixture), PlumbingFixture},
      { nameof(Roof), Roof },
      { nameof(Railing), Railing },
      { nameof(StructuralFraming), StructuralFraming },
      { nameof(Wall), Wall },
      { nameof(Window), Window },
      { nameof(Wire), Wire },
      { nameof(Undefined), Undefined },
    };
  }
  public static RevitCategoryInfo CableTray { get; } = new(
    nameof(CableTray),
    typeof(DB.Electrical.CableTray),
    typeof(DB.Electrical.CableTrayType),
    new List<BuiltInCategory>()
  );
  public static RevitCategoryInfo Ceiling { get; } = new(
    nameof(Ceiling),
    typeof(DB.Ceiling),
    typeof(CeilingType),
    new List<BuiltInCategory>()
  );
  public static RevitCategoryInfo Column { get; } = new(
    nameof(Column),
    typeof(FamilyInstance),
    typeof(FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_StructuralColumns
    });
  public static RevitCategoryInfo Conduit { get; } = new(
    nameof(Conduit),
    typeof(DB.Electrical.Conduit),
    typeof(DB.Electrical.ConduitType),
    new List<BuiltInCategory>()
  );
  public static RevitCategoryInfo Door { get; } = new(
    nameof(Door),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Doors
    });
  public static RevitCategoryInfo Duct { get; } = new(
    nameof(Duct),
    typeof(DB.Mechanical.Duct),
    typeof(DB.MEPCurveType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_FlexDuctCurves
    });
  public static RevitCategoryInfo FamilyInstance { get; } = new(
    nameof(FamilyInstance),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>()
    );
  public static RevitCategoryInfo Floor { get; } = new(
    nameof(Floor),
    typeof(DB.Floor),
    typeof(DB.FloorType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Floors
    });
  public static RevitCategoryInfo Furniture { get; } = new(
    nameof(Furniture),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Furniture
    });
  //public static RevitCategoryInfo Material { get; } = new(
  //  nameof(Material), 
  //  typeof(DB.Material), 
  //  null,
  //  new List<BuiltInCategory> 
  //  { 
  //    BuiltInCategory.OST_Materials, 
  //    BuiltInCategory.OST_PipeMaterials,
  //    BuiltInCategory.OST_WireMaterials
  //  });
  public static RevitCategoryInfo Pipe { get; } = new(
    nameof(Pipe),
    typeof(DB.Plumbing.Pipe),
    typeof(DB.MEPCurveType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_FlexPipeCurves
    });
  public static RevitCategoryInfo PlumbingFixture { get; } = new(
    nameof(PlumbingFixture),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_PlumbingFixtures
    });
  public static RevitCategoryInfo Roof { get; } = new(
    nameof(Roof),
    typeof(DB.RoofBase),
    typeof(DB.RoofType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Roofs,
    });
  public static RevitCategoryInfo Railing { get; } = new(
    nameof(Railing),
    typeof(DB.Architecture.Railing),
    typeof(DB.Architecture.RailingType),
    new List<BuiltInCategory>()
    );
  public static RevitCategoryInfo StructuralFraming { get; } = new(
    nameof(StructuralFraming),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_StructuralFraming
    },
    new List<string> { "beam", "brace", "framing" });
  public static RevitCategoryInfo Wall { get; } = new(
    nameof(Wall),
    typeof(DB.Wall),
    typeof(DB.WallType),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Walls
    });
  public static RevitCategoryInfo Window { get; } = new(
    nameof(Window),
    typeof(DB.FamilyInstance),
    typeof(DB.FamilySymbol),
    new List<BuiltInCategory>
    {
        BuiltInCategory.OST_Windows
    });
  public static RevitCategoryInfo Wire { get; } = new(
    nameof(Wire),
    typeof(DB.Electrical.Wire),
    typeof(DB.Electrical.WireType),
    new List<BuiltInCategory>()
    );
  public static RevitCategoryInfo Undefined { get; } = new(
    nameof(Undefined),
    null,
    null,
    new List<BuiltInCategory>()
    );
}
